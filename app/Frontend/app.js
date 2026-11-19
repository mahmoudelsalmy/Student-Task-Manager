(() => {
  "use strict";

  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const state = {
    token: sessionStorage.getItem("taskmate.accessToken"),
    user: null,
    authMode: "login",
    view: "all",
    page: 1,
    pageSize: 20,
    totalPages: 0,
    totalCount: 0, courses: [], searchTimer: null,
    planningDate: new Date(new Date().getFullYear(), new Date().getMonth(), 1),
    loadId: 0,
    toastTimer: null,
  };

  const labels = {
    status: { ToDo: "To do", InProgress: "In progress", Completed: "Done" },
    category: { Assignment: "Assignment", Exam: "Exam", Project: "Project", Personal: "Personal" },
    priority: { High: "High", Medium: "Medium", Low: "Low" },
  };

  function localDateString(date = new Date()) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  }

  function displayDate(dateString, options = { month: "short", day: "numeric" }) {
    if (!dateString) return "No due date";
    const [year, month, day] = dateString.split("-").map(Number);
    return new Intl.DateTimeFormat(undefined, options).format(new Date(year, month - 1, day));
  }

  function setMessage(element, message = "") {
    element.textContent = message;
    element.hidden = !message;
  }

  function formatValidationErrors(errors) {
    const fieldLabels = { name: "Name", email: "Email address", password: "Password" };
    return Object.entries(errors).flatMap(([field, fieldErrors]) => {
      const label = fieldLabels[field.toLowerCase()] || field;
      return fieldErrors.map(message => {
        if (field.toLowerCase() === "email" && /not a valid e-mail address/i.test(message)) {
          return "Please enter a valid email address.";
        }
        if (field.toLowerCase() === "password" && /minimum length of 12/i.test(message)) {
          return "Password must be at least 12 characters.";
        }
        if (/required/i.test(message)) return `${label} is required.`;
        return `${label}: ${message}`;
      });
    }).join(" ");
  }

  function showToast(message, isError = false) {
    const toast = $("#toast");
    toast.textContent = message;
    toast.classList.toggle("error", isError);
    toast.classList.add("show");
    clearTimeout(state.toastTimer);
    state.toastTimer = setTimeout(() => toast.classList.remove("show"), 3200);
  }

  async function api(path, options = {}) {
    const headers = new Headers(options.headers || {});
    if (options.body !== undefined) headers.set("Content-Type", "application/json");
    if (state.token) headers.set("Authorization", `Bearer ${state.token}`);

    let response;
    try {
      response = await fetch(path, { ...options, headers });
    } catch {
      throw new Error("Can’t reach TaskMate right now. Check your connection and try again.");
    }

    if (response.status === 401 && state.token) {
      signOut(false);
      throw new Error("Your session has ended. Please sign in again.");
    }
    if (response.status === 204) return null;

    const contentType = response.headers.get("content-type") || "";
    // ASP.NET ProblemDetails responses use application/problem+json.
    const payload = contentType.includes("json") ? await response.json() : null;
    if (!response.ok) {
      const validation = payload?.errors && formatValidationErrors(payload.errors);
      throw new Error(validation || payload?.detail || payload?.title || "Something went wrong. Please try again.");
    }
    return payload;
  }

  function showAuth() {
    $("#auth-view").classList.remove("hidden");
    $("#app-view").classList.add("hidden");
    $("#auth-email").value = "";
    $("#auth-password").value = "";
    $("#auth-name").value = "";
    setMessage($("#auth-message"));
    setAuthMode("login");
  }

  function signOut(showNotice = true) {
    state.token = null;
    state.user = null;
    sessionStorage.removeItem("taskmate.accessToken");
    showAuth();
    if (showNotice) showToast("You’re signed out. See you soon.");
  }

  function setAuthMode(mode) {
    state.authMode = mode;
    const registering = mode === "register";
    $("#login-tab").classList.toggle("active", !registering);
    $("#register-tab").classList.toggle("active", registering);
    $("#login-tab").setAttribute("aria-selected", String(!registering));
    $("#register-tab").setAttribute("aria-selected", String(registering));
    $("#name-field").classList.toggle("hidden", !registering);
    $("#auth-name").required = registering;
    $("#auth-password").setAttribute("autocomplete", registering ? "new-password" : "current-password");
    $("#auth-eyebrow").textContent = registering ? "A FRESH START" : "WELCOME BACK";
    $("#auth-title").textContent = registering ? "Let’s get you organized." : "Good to see you.";
    $("#auth-subtitle").textContent = registering
      ? "Create your free account and make space for what matters."
      : "Sign in and pick up right where you left off.";
    $("#password-hint").classList.toggle("hidden", !registering);
    $("#auth-submit").querySelector("span:first-child").textContent = registering ? "Create my account" : "Sign in";
    setMessage($("#auth-message"));
  }

  async function completeSignIn(payload) {
    state.token = payload.accessToken;
    state.user = payload.user;
    sessionStorage.setItem("taskmate.accessToken", payload.accessToken);
    $("#auth-view").classList.add("hidden");
    $("#app-view").classList.remove("hidden");
    $("#profile-name").textContent = payload.user.name;
    $("#profile-email").textContent = payload.user.email;
    $("#avatar").textContent = [...payload.user.name.trim()][0]?.toUpperCase() || "S";
    setWelcome();
    await refreshDashboard();
    await Promise.all([loadTasks(), loadCourses()]);
  }

  function setWelcome() {
    const now = new Date();
    const hour = now.getHours();
    const greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
    const firstName = state.user?.name?.trim().split(/\s+/)[0] || "there";
    $("#welcome-date").textContent = new Intl.DateTimeFormat(undefined, { weekday: "long", month: "long", day: "numeric" }).format(now).toUpperCase();
    $("#today-label").textContent = new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric" }).format(now);
    $("#welcome-title").textContent = `${greeting}, ${firstName}.`;
    $("#welcome-copy").textContent = hour < 11 ? "Let’s make today feel a little more manageable." : "Here’s what’s on your plate. You’ve got this.";
  }

  async function refreshDashboard() {
    const stats = await api(`/api/dashboard?today=${encodeURIComponent(localDateString())}`);
    $("#stat-total").textContent = stats.totalTasks;
    $("#stat-completed").textContent = stats.completedTasks;
    $("#stat-upcoming").textContent = stats.upcomingDeadlines;
    $("#stat-overdue").textContent = stats.overdueTasks;
    $("#completion-percent").textContent = `${stats.completionPercent}%`;
    $("#nav-total").textContent = stats.totalTasks;
    $("#nav-upcoming").textContent = stats.upcomingDeadlines;
  }

  function createSvg(pathMarkup, viewBox = "0 0 24 24") {
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("viewBox", viewBox);
    svg.setAttribute("aria-hidden", "true");
    svg.innerHTML = pathMarkup;
    return svg;
  }

  function createTaskRow(task) {
    const row = document.createElement("article");
    row.className = `task-row${task.status === "Completed" ? " is-complete" : ""}`;
    row.dataset.id = task.id;

    const check = document.createElement("button");
    check.className = `task-check${task.status === "Completed" ? " checked" : ""}`;
    check.type = "button";
    check.setAttribute("aria-label", task.status === "Completed" ? `Mark ${task.title} incomplete` : `Mark ${task.title} complete`);
    check.setAttribute("aria-pressed", String(task.status === "Completed"));
    check.addEventListener("click", () => updateTask(task, { status: task.status === "Completed" ? "ToDo" : "Completed" }));

    const main = document.createElement("div");
    main.className = "task-main";
    const title = document.createElement("p");
    title.className = "task-title";
    title.textContent = task.title;
    const description = document.createElement("p");
    description.className = "task-description";
    description.textContent = task.description || task.category;
    main.append(title, description);

    const category = document.createElement("div");
    category.className = "category-cell";
    const dot = document.createElement("span");
    dot.className = `category-dot ${task.category.toLowerCase()}`;
    const categoryLabel = document.createElement("span");
    categoryLabel.textContent = labels.category[task.category] || task.category;
    category.append(dot, categoryLabel);

    const due = document.createElement("div");
    const isOverdue = task.dueDate && task.dueDate < localDateString() && task.status !== "Completed";
    due.className = `due-cell${isOverdue ? " overdue" : ""}${task.dueDate ? "" : " no-date"}`;
    due.append(createSvg("<rect x=\"3.5\" y=\"5.5\" width=\"17\" height=\"15\" rx=\"2.5\"/><path d=\"M7.5 3.5v4M16.5 3.5v4M3.5 10h17\"/>"));
    const dueLabel = document.createElement("span");
    dueLabel.textContent = task.dueDate ? (isOverdue ? `Overdue · ${displayDate(task.dueDate)}` : displayDate(task.dueDate)) : "No date";
    due.append(dueLabel);

    const priority = document.createElement("span");
    priority.className = `priority-pill priority-${task.priority.toLowerCase()}`;
    priority.textContent = labels.priority[task.priority] || task.priority;

    const statusCell = document.createElement("div");
    statusCell.className = "status-cell";
    const status = document.createElement("button");
    status.type = "button";
    status.className = `status-pill status-${task.status.toLowerCase()}`;
    status.textContent = labels.status[task.status] || task.status;
    status.setAttribute("aria-label", `Change status for ${task.title}. Current status: ${labels.status[task.status]}`);
    status.title = "Click to change status";
    status.addEventListener("click", () => {
      const next = { ToDo: "InProgress", InProgress: "Completed", Completed: "ToDo" }[task.status] || "ToDo";
      updateTask(task, { status: next });
    });
    statusCell.append(status);

    const actions = document.createElement("div");
    actions.className = "task-actions";
    const edit = document.createElement("button");
    edit.type = "button";
    edit.className = "task-action";
    edit.setAttribute("aria-label", `Edit ${task.title}`);
    edit.title = "Edit task";
    edit.append(createSvg("<path d=\"m14 5 5 5M4 20l4.2-.9L19.3 8a2.1 2.1 0 0 0-3-3L5.2 16.1 4 20Z\"/>"));
    edit.addEventListener("click", () => openTaskDialog(task));
    const remove = document.createElement("button");
    remove.type = "button";
    remove.className = "task-action delete";
    remove.setAttribute("aria-label", `Delete ${task.title}`);
    remove.title = "Delete task";
    remove.append(createSvg("<path d=\"M4 7h16M10 11v6m4-6v6M6 7l1 13h10l1-13M9 7V4h6v3\"/>"));
    remove.addEventListener("click", () => deleteTask(task));
    actions.append(edit, remove);

    row.append(check, main, category, due, priority, statusCell, actions);
    return row;
  }

  function makeTaskQuery() {
    const params = new URLSearchParams();
    const chosenStatus = $("#status-filter").value || (state.view === "completed" ? "Completed" : "");
    if (chosenStatus) params.set("status", chosenStatus);
    if ($("#category-filter").value) params.set("category", $("#category-filter").value);
    if ($("#priority-filter").value) params.set("priority", $("#priority-filter").value);
    if ($("#task-search").value.trim()) params.set("search", $("#task-search").value.trim());
    if (state.view === "upcoming") {
      const today = localDateString();
      const week = new Date();
      week.setDate(week.getDate() + 7);
      params.set("dueFrom", today);
      params.set("dueThrough", localDateString(week));
      params.set("excludeCompleted", "true");
    }
    params.set("page", String(state.page));
    params.set("pageSize", String(state.pageSize));
    return params;
  }

  async function loadTasks() {
    const loadId = ++state.loadId;
    const list = $("#task-list");
    list.setAttribute("aria-busy", "true");
    list.innerHTML = "<div class=\"loading-state\">Gathering your tasks…</div>";
    $("#task-empty").classList.add("hidden");
    $("#pagination").classList.add("hidden");
    try {
      const params = makeTaskQuery();
      const result = await api(`/api/tasks?${params.toString()}`);
      if (loadId !== state.loadId) return;
      state.totalPages = result.totalPages;
      state.totalCount = result.totalCount;
      if (state.totalPages > 0 && state.page > state.totalPages) {
        state.page = state.totalPages;
        await loadTasks();
        return;
      }
      $("#task-count").textContent = result.totalCount;
      list.replaceChildren(...result.items.map(createTaskRow));
      setEmptyState(result.totalCount === 0);
      setPagination(result);
    } catch (error) {
      if (loadId !== state.loadId) return;
      list.replaceChildren();
      setEmptyState(true, "We couldn’t load your tasks.", error.message);
      showToast(error.message, true);
    } finally {
      if (loadId === state.loadId) list.setAttribute("aria-busy", "false");
    }
  }

  function setEmptyState(isEmpty, title, copy) {
    $("#task-empty").classList.toggle("hidden", !isEmpty);
    if (!isEmpty) return;
    const hasFilters = Boolean($("#task-search").value || $("#status-filter").value || $("#category-filter").value || $("#priority-filter").value || state.view !== "all");
    $("#empty-title").textContent = title || (hasFilters ? "No tasks match those filters." : "Your list is clear.");
    $("#empty-copy").textContent = copy || (hasFilters ? "Try changing your search or clearing a filter." : "Add your first task and give your week a little shape.");
    $("#empty-add").classList.toggle("hidden", hasFilters || Boolean(title));
  }

  function setPagination(result) {
    const pagination = $("#pagination");
    const show = result.totalCount > 0;
    pagination.classList.toggle("hidden", !show);
    const first = result.totalCount ? (result.page - 1) * result.pageSize + 1 : 0;
    const last = Math.min(result.page * result.pageSize, result.totalCount);
    $("#page-summary").textContent = `Showing ${first}–${last} of ${result.totalCount}`;
    $("#page-prev").disabled = result.page <= 1;
    $("#page-next").disabled = result.page >= result.totalPages;
  }

  async function saveTask(event) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    const id = $("#task-id").value;
    const payload = {
      title: $("#task-title").value.trim(),
      description: $("#task-description").value.trim() || null,
      dueDate: $("#task-due").value || null,
      priority: $("#task-priority").value,
      status: $("#task-status").value,
      category: $("#task-category").value,
      courseId: $("#task-course").value || null,
    };
    const saveButton = $("#task-save");
    saveButton.disabled = true;
    saveButton.textContent = id ? "Saving…" : "Creating…";
    setMessage($("#task-form-message"));
    try {
      await api(id ? `/api/tasks/${id}` : "/api/tasks", {
        method: id ? "PUT" : "POST",
        body: JSON.stringify(payload),
      });
      $("#task-dialog").close();
      state.page = 1;
      await Promise.all([refreshCurrentView(), refreshDashboard()]);
      showToast(id ? "Your task is updated." : "Your task is on the list.");
    } catch (error) {
      setMessage($("#task-form-message"), error.message);
    } finally {
      saveButton.disabled = false;
      saveButton.innerHTML = "Save task <span aria-hidden=\"true\">→</span>";
    }
  }

  function openTaskDialog(task = null) {
    $("#task-form").reset();
    $("#task-id").value = task?.id || "";
    $("#dialog-title").textContent = task ? "Edit task" : "Add a task";
    $("#task-title").value = task?.title || "";
    $("#task-description").value = task?.description || "";
    $("#task-due").value = task?.dueDate || "";
    $("#task-category").value = task?.category || "Assignment";
    $("#task-course").value = task?.courseId || "";
    $("#task-priority").value = task?.priority || "Medium";
    $("#task-status").value = task?.status || "ToDo";
    setMessage($("#task-form-message"));
    $("#task-dialog").showModal();
    requestAnimationFrame(() => $("#task-title").focus());
  }

  async function updateTask(task, changes) {
    const payload = {
      title: task.title,
      description: task.description,
      dueDate: task.dueDate,
      priority: task.priority,
      status: changes.status || task.status,
      category: task.category,
      courseId: task.courseId || null,
    };
    try {
      await api(`/api/tasks/${task.id}`, { method: "PUT", body: JSON.stringify(payload) });
      await Promise.all([refreshCurrentView(), refreshDashboard()]);
      showToast(changes.status === "Completed" ? "One less thing on your mind. Nice work." : "Task status updated.");
    } catch (error) {
      showToast(error.message, true);
    }
  }

  async function deleteTask(task) {
    if (!window.confirm(`Delete “${task.title}”? This can’t be undone.`)) return;
    try {
      await api(`/api/tasks/${task.id}`, { method: "DELETE" });
      await Promise.all([refreshCurrentView(), refreshDashboard()]);
      showToast("Task deleted.");
    } catch (error) {
      showToast(error.message, true);
    }
  }


function courseCard(c){const el=document.createElement("article");el.className="course-card";el.style.setProperty("--course-color",c.color||"#64748b");const name=document.createElement("h3");name.textContent=c.name;const meta=document.createElement("p");meta.textContent=[c.code,c.instructor,c.semester].filter(Boolean).join(" · ");const edit=document.createElement("button");edit.type="button";edit.className="button button-outline button-small";edit.textContent="Edit";edit.addEventListener("click",()=>courseDialog(c));const del=document.createElement("button");del.type="button";del.className="button button-quiet button-small";del.textContent="Delete";del.addEventListener("click",()=>deleteCourse(c));el.append(name,meta,edit,del);return el;}
function updateCourseOptions(){const select=$("#task-course");const selected=select.value;select.replaceChildren(new Option("No course", ""),...state.courses.map(c=>new Option(c.code?`${c.name} (${c.code})`:c.name,c.id)));select.value=state.courses.some(c=>c.id===selected)?selected:"";}
async function loadCourses(){try{state.courses=await api("/api/courses");updateCourseOptions();$("#course-count").textContent=state.courses.length;$("#nav-courses").textContent=state.courses.length;$("#course-list").replaceChildren(...state.courses.map(courseCard));$("#course-empty").classList.toggle("hidden",state.courses.length>0);}catch(e){showToast(e.message,true);}}
function courseDialog(c=null){$("#course-form").reset();$("#course-id").value=c?.id||"";$("#course-dialog-title").textContent=c?"Edit course":"Add a course";$("#course-name").value=c?.name||"";$("#course-code").value=c?.code||"";$("#course-instructor").value=c?.instructor||"";$("#course-semester").value=c?.semester||"";$("#course-color").value=c?.color||"#64748b";setMessage($("#course-form-message"));$("#course-dialog").showModal();}
async function saveCourse(e){e.preventDefault();const id=$("#course-id").value;const body={name:$("#course-name").value.trim(),code:$("#course-code").value.trim()||null,instructor:$("#course-instructor").value.trim()||null,semester:$("#course-semester").value.trim()||null,color:$("#course-color").value};try{await api(id?`/api/courses/${id}`:"/api/courses",{method:id?"PUT":"POST",body:JSON.stringify(body)});$("#course-dialog").close();await loadCourses();showToast("Course saved.");}catch(err){setMessage($("#course-form-message"),err.message);}}
async function deleteCourse(c){if(!confirm(`Delete “${c.name}”?`))return;try{await api(`/api/courses/${c.id}`,{method:"DELETE"});await Promise.all([loadCourses(),refreshCurrentView()]);showToast("Course deleted. Linked tasks are now uncategorized.");}catch(e){showToast(e.message,true);}}
function showCourses(){state.view="courses";$(".nav-link").forEach(b=>b.classList.toggle("active",b.dataset.view==="courses"));$("#breadcrumb-current").textContent="My courses";$(".stats-grid").classList.add("hidden");$(".welcome-row").classList.add("hidden");$(".tasks-panel").classList.add("hidden");$("#planning-panel").classList.add("hidden");$("#courses-panel").classList.remove("hidden");loadCourses();closeSidebar();}
async function getAllTasks(extra={}){const all=[];let page=1,totalPages=1;do{const params=new URLSearchParams({page:String(page),pageSize:"100",...extra});const result=await api(`/api/tasks?${params}`);all.push(...result.items);totalPages=result.totalPages;page++;}while(page<=totalPages&&page<=20);return all;}
function taskCourseName(task){const course=state.courses.find(item=>item.id===task.courseId);return course?(course.code?`${course.name} · ${course.code}`:course.name):"No course";}
function makePlanningTask(task,extraClass=""){const card=document.createElement("article");card.className=`planning-task ${extraClass}`.trim();const open=document.createElement("button");open.type="button";open.className="planning-task-title";open.textContent=task.title;open.addEventListener("click",()=>openTaskDialog(task));const detail=document.createElement("p");detail.textContent=`${taskCourseName(task)} · ${labels.category[task.category]||task.category}${task.dueDate?` · Due ${displayDate(task.dueDate)}`:""}`;card.append(open,detail);return card;}
async function renderPlanningView(){const panel=$("#planning-content");panel.setAttribute("aria-busy","true");panel.textContent="Loading your plan…";const date=new Date(state.planningDate);const isCalendar=state.view==="calendar",isPlanner=state.view==="planner";let from,to;
if(isCalendar){date.setDate(1);from=new Date(date.getFullYear(),date.getMonth(),1);to=new Date(date.getFullYear(),date.getMonth()+1,0);$("#planning-heading").textContent="Calendar";$("#planning-description").textContent="Your exams, assignments, and deadlines by day.";$("#planning-kicker").textContent="DEADLINES AT A GLANCE";$("#planning-period").textContent=new Intl.DateTimeFormat(undefined,{month:"long",year:"numeric"}).format(date);}
else if(isPlanner){from=new Date();from.setHours(0,0,0,0);to=new Date(from);to.setDate(to.getDate()+6);$("#planning-heading").textContent="Study planner";$("#planning-description").textContent="A focused week, paced around your upcoming work.";$("#planning-kicker").textContent="MAKE SPACE TO STUDY";$("#planning-period").textContent=`${displayDate(localDateString(from))} – ${displayDate(localDateString(to))}`;}
else{$("#planning-heading").textContent="Kanban board";$("#planning-description").textContent="Move work forward by changing its status.";$("#planning-kicker").textContent="YOUR WORK IN PROGRESS";$("#planning-period").textContent="";}
try{const tasks=isCalendar?await getAllTasks({dueFrom:localDateString(from),dueThrough:localDateString(to)}):isPlanner?await getAllTasks({dueFrom:localDateString(from),dueThrough:localDateString(to),excludeCompleted:"true"}):await getAllTasks();panel.replaceChildren();
if(isCalendar){const grid=document.createElement("div");grid.className="calendar-grid";["Mon","Tue","Wed","Thu","Fri","Sat","Sun"].forEach(name=>{const label=document.createElement("div");label.className="calendar-weekday";label.textContent=name;grid.append(label);});const offset=(from.getDay()+6)%7;for(let i=0;i<offset;i++){const blank=document.createElement("div");blank.className="calendar-day outside-month";blank.setAttribute("aria-hidden","true");grid.append(blank);}for(let day=1;day<=to.getDate();day++){const cell=document.createElement("div"),stamp=localDateString(new Date(from.getFullYear(),from.getMonth(),day));cell.className="calendar-day";if(stamp===localDateString())cell.classList.add("today");const number=document.createElement("span");number.className="calendar-date";number.textContent=day;cell.append(number);tasks.filter(task=>task.dueDate===stamp).forEach(task=>{const item=document.createElement("button");item.type="button";item.className=`calendar-event event-${task.category.toLowerCase()}`;item.textContent=task.title;item.title=`${task.title} · ${taskCourseName(task)}`;item.addEventListener("click",()=>openTaskDialog(task));cell.append(item);});grid.append(cell);}panel.append(grid);if(!tasks.length){const note=document.createElement("p");note.className="planning-note";note.textContent="No deadlines this month. Add an exam or assignment to start filling your calendar.";panel.append(note);}}
else if(isPlanner){const days=document.createElement("div");days.className="planner-days";for(let i=0;i<7;i++){const current=new Date(from);current.setDate(from.getDate()+i);const stamp=localDateString(current),group=document.createElement("section");group.className="planner-day";const heading=document.createElement("h3");heading.textContent=new Intl.DateTimeFormat(undefined,{weekday:"long",month:"short",day:"numeric"}).format(current);const dayTasks=tasks.filter(task=>task.dueDate===stamp);const totalMinutes=dayTasks.reduce((sum,task)=>sum+(task.category==="Exam"?60:task.category==="Assignment"?45:30),0);const summary=document.createElement("span");summary.textContent=dayTasks.length?`${dayTasks.length} item${dayTasks.length===1?"":"s"} · ${totalMinutes} min suggested`:"Open time";const title=document.createElement("div");title.className="planner-day-heading";title.append(heading,summary);group.append(title);dayTasks.forEach(task=>group.append(makePlanningTask(task)));if(!dayTasks.length){const free=document.createElement("p");free.className="planning-note";free.textContent="No deadlines scheduled. Use this space for review or a break.";group.append(free);}days.append(group);}panel.append(days);if(!tasks.length){const note=document.createElement("p");note.className="planning-note";note.textContent="Your next seven days are clear. Add an exam or assignment deadline and it will appear here.";panel.prepend(note);}}
else{const board=document.createElement("div");board.className="kanban-board";for(const [status,title] of [["ToDo","To do"],["InProgress","In progress"],["Completed","Completed"]]){const column=document.createElement("section");column.className="kanban-column";const header=document.createElement("div"),h=document.createElement("h3"),count=document.createElement("span");header.className="kanban-column-heading";h.textContent=title;count.textContent=tasks.filter(task=>task.status===status).length;header.append(h,count);column.append(header);tasks.filter(task=>task.status===status).forEach(task=>{const card=makePlanningTask(task,`priority-${task.priority.toLowerCase()}`),select=document.createElement("select");select.setAttribute("aria-label",`Move ${task.title}`);[["ToDo","To do"],["InProgress","In progress"],["Completed","Completed"]].forEach(([value,label])=>select.add(new Option(label,value)));select.value=task.status;select.addEventListener("change",()=>updateTask(task,{status:select.value}));card.append(select);column.append(card);});board.append(column);}panel.append(board);if(!tasks.length)panel.textContent="Nothing here yet. Add a task and it will appear on your board.";}
}catch(error){panel.textContent=`We couldn’t load this view. ${error.message}`;showToast(error.message,true);}finally{panel.setAttribute("aria-busy","false");}}
function refreshCurrentView(){return ["calendar","kanban","planner"].includes(state.view)?renderPlanningView():loadTasks();}
  function setView(view) {
    state.view = view;
    state.page = 1;
    // Navigation views own the status filter; do not let a previous manual
    // filter (for example InProgress) override the Completed view.
    $("#status-filter").value = "";
    $$(".nav-link").forEach(button => button.classList.toggle("active", button.dataset.view === view));
    if (["calendar", "kanban", "planner"].includes(view)) {
      const title = { calendar: "Calendar", kanban: "Kanban board", planner: "Study planner" }[view];
      $("#breadcrumb-current").textContent = title;
      $(".stats-grid").classList.add("hidden"); $(".welcome-row").classList.add("hidden");
      $(".tasks-panel").classList.add("hidden"); $("#courses-panel").classList.add("hidden");
      $("#planning-panel").classList.remove("hidden");
      $("#planning-prev").classList.toggle("hidden", view !== "calendar");
      $("#planning-next").classList.toggle("hidden", view !== "calendar");
      renderPlanningView(); closeSidebar(); return;
    }
    $("#planning-panel").classList.add("hidden");
    const heading = view === "upcoming" ? "Upcoming" : view === "completed" ? "Completed" : "All tasks";
    $("#breadcrumb-current").textContent = heading;
    $("#tasks-heading").firstChild.textContent = `${heading} `;$(".stats-grid").classList.remove("hidden");$(".welcome-row").classList.remove("hidden");$(".tasks-panel").classList.remove("hidden");$("#courses-panel").classList.add("hidden");
    loadTasks();
    closeSidebar();
  }

  function closeSidebar() {
    $("#sidebar").classList.remove("open");
    $("#mobile-menu").setAttribute("aria-label", "Open navigation");
  }

  function bindEvents() {
    $("#login-tab").addEventListener("click", () => setAuthMode("login"));
    $("#register-tab").addEventListener("click", () => setAuthMode("register"));
    $("#auth-form").addEventListener("submit", async event => {
      event.preventDefault();
      const registering = state.authMode === "register";
      const button = $("#auth-submit");
      const original = registering ? "Create my account" : "Sign in";
      button.disabled = true;
      button.querySelector("span:first-child").textContent = registering ? "Creating your account…" : "Signing in…";
      setMessage($("#auth-message"));
      const body = {
        email: $("#auth-email").value.trim(),
        password: $("#auth-password").value,
      };
      if (registering) body.name = $("#auth-name").value.trim();
      try {
        const payload = await api(`/api/auth/${registering ? "register" : "login"}`, {
          method: "POST",
          body: JSON.stringify(body),
        });
        await completeSignIn(payload);
      } catch (error) {
        setMessage($("#auth-message"), error.message);
      } finally {
        button.disabled = false;
        button.querySelector("span:first-child").textContent = original;
      }
    });

    $("#logout-button").addEventListener("click", () => signOut());
    $("#add-task-top").addEventListener("click", () => openTaskDialog());
    $("#add-task-panel").addEventListener("click", () => openTaskDialog());
    $("#empty-add").addEventListener("click", () => openTaskDialog());
    $("#dialog-close").addEventListener("click", () => $("#task-dialog").close());
    $("#dialog-cancel").addEventListener("click", () => $("#task-dialog").close());
    $("#task-form").addEventListener("submit", saveTask);
    $("#task-dialog").addEventListener("click", event => {
      if (event.target === $("#task-dialog")) $("#task-dialog").close();
    });

    $(".nav-link").forEach(button => button.addEventListener("click", () => button.dataset.view==="courses"?showCourses():setView(button.dataset.view)));
$("#add-course").addEventListener("click",()=>courseDialog());$("#course-empty-add").addEventListener("click",()=>courseDialog());$("#course-form").addEventListener("submit",saveCourse);$("#course-cancel").addEventListener("click",()=>$("#course-dialog").close());
    $("#planning-prev").addEventListener("click", () => { state.planningDate = new Date(state.planningDate.getFullYear(), state.planningDate.getMonth() - 1, 1); renderPlanningView(); });
    $("#planning-next").addEventListener("click", () => { state.planningDate = new Date(state.planningDate.getFullYear(), state.planningDate.getMonth() + 1, 1); renderPlanningView(); });
    ["#status-filter", "#category-filter", "#priority-filter"].forEach(selector => {
      $(selector).addEventListener("change", () => {
        state.view = "all";
        state.page = 1;
        $$(".nav-link").forEach(button => button.classList.toggle("active", button.dataset.view === "all"));
        $("#breadcrumb-current").textContent = "All tasks";
        $("#tasks-heading").firstChild.textContent = "All tasks ";
        loadTasks();
      });
    });
    $("#task-search").addEventListener("input", () => {
      state.page = 1;
      clearTimeout(state.searchTimer);
      state.searchTimer = setTimeout(loadTasks, 220);
    });
    $("#clear-filters").addEventListener("click", () => {
      $("#status-filter").value = "";
      $("#category-filter").value = "";
      $("#priority-filter").value = "";
      $("#task-search").value = "";
      setView("all");
    });
    $("#page-prev").addEventListener("click", () => { if (state.page > 1) { state.page -= 1; loadTasks(); } });
    $("#page-next").addEventListener("click", () => { if (state.page < state.totalPages) { state.page += 1; loadTasks(); } });
    $("#mobile-menu").addEventListener("click", () => {
      const open = $("#sidebar").classList.toggle("open");
      $("#mobile-menu").setAttribute("aria-label", open ? "Close navigation" : "Open navigation");
    });
    $(".main-column").addEventListener("click", event => {
      if (window.innerWidth <= 720 && $("#sidebar").classList.contains("open") && !event.target.closest("#mobile-menu")) closeSidebar();
    });
    document.addEventListener("keydown", event => {
      if (event.key === "/" && !["INPUT", "TEXTAREA", "SELECT"].includes(document.activeElement?.tagName) && !$("#task-dialog").open) {
        event.preventDefault();
        $("#task-search").focus();
      }
      if (event.key === "Escape") closeSidebar();
    });
  }

  async function initialize() {
    const year = String(new Date().getFullYear());
    $("#year").textContent = year;
    $("#footer-year").textContent = year;
    bindEvents();
    if (!state.token) {
      showAuth();
      return;
    }
    try {
      state.user = await api("/api/auth/me");
      $("#auth-view").classList.add("hidden");
      $("#app-view").classList.remove("hidden");
      $("#profile-name").textContent = state.user.name;
      $("#profile-email").textContent = state.user.email;
      $("#avatar").textContent = [...state.user.name.trim()][0]?.toUpperCase() || "S";
      setWelcome();
      await Promise.all([refreshDashboard(), loadTasks(), loadCourses()]);
    } catch (error) {
      if (state.token) {
        signOut(false);
        setMessage($("#auth-message"), error.message);
      }
    }
  }

  initialize();
})();
