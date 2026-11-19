CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Users" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "NormalizedEmail" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);

CREATE TABLE "Courses" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Courses" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Code" TEXT NULL,
    "Instructor" TEXT NULL,
    "Semester" TEXT NULL,
    "Color" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Courses_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Tasks" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Tasks" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NULL,
    "DueDate" TEXT NULL,
    "Priority" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "CourseId" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Tasks_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Tasks_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE SET NULL
);

CREATE TABLE "StudySessions" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_StudySessions" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "CourseId" TEXT NULL,
    "TaskId" TEXT NULL,
    "StartedAtUtc" TEXT NOT NULL,
    "EndedAtUtc" TEXT NOT NULL,
    "DurationMinutes" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_StudySessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_StudySessions_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_StudySessions_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Exams" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Exams" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "CourseId" TEXT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NULL,
    "ScheduledAtUtc" TEXT NOT NULL,
    "DurationMinutes" INTEGER NOT NULL,
    "Location" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Exams_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Exams_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE SET NULL
);

CREATE TABLE "StudyGoals" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_StudyGoals" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "CourseId" TEXT NULL,
    "Title" TEXT NOT NULL,
    "TargetMinutesPerWeek" INTEGER NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_StudyGoals_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_StudyGoals_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_StudyGoals_UserId" ON "StudyGoals" ("UserId");
CREATE INDEX "IX_StudyGoals_CourseId" ON "StudyGoals" ("CourseId");

CREATE INDEX "IX_Exams_UserId_ScheduledAtUtc" ON "Exams" ("UserId", "ScheduledAtUtc");
CREATE INDEX "IX_Exams_CourseId" ON "Exams" ("CourseId");

CREATE INDEX "IX_Courses_UserId_Name" ON "Courses" ("UserId", "Name");

CREATE UNIQUE INDEX "IX_Courses_UserId_Code" ON "Courses" ("UserId", "Code");

CREATE INDEX "IX_Tasks_UserId_DueDate" ON "Tasks" ("UserId", "DueDate");

CREATE INDEX "IX_Tasks_UserId_Status" ON "Tasks" ("UserId", "Status");

CREATE INDEX "IX_Tasks_CourseId" ON "Tasks" ("CourseId");

CREATE INDEX "IX_StudySessions_UserId_StartedAtUtc" ON "StudySessions" ("UserId", "StartedAtUtc");

CREATE INDEX "IX_StudySessions_CourseId" ON "StudySessions" ("CourseId");

CREATE INDEX "IX_StudySessions_TaskId" ON "StudySessions" ("TaskId");

CREATE UNIQUE INDEX "IX_Users_NormalizedEmail" ON "Users" ("NormalizedEmail");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261022193024_InitialCreate', '10.0.13');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119170000_AddCourses', '10.0.13');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119173000_LinkTasksToCourses', '10.0.13');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119190000_AddStudySessions', '10.0.13');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119210000_AddExams', '10.0.13');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119220000_AddStudyGoals', '10.0.13');

COMMIT;

