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

CREATE INDEX "IX_Courses_UserId_Name" ON "Courses" ("UserId", "Name");

CREATE UNIQUE INDEX "IX_Courses_UserId_Code" ON "Courses" ("UserId", "Code");

CREATE INDEX "IX_Tasks_UserId_DueDate" ON "Tasks" ("UserId", "DueDate");

CREATE INDEX "IX_Tasks_UserId_Status" ON "Tasks" ("UserId", "Status");

CREATE INDEX "IX_Tasks_CourseId" ON "Tasks" ("CourseId");

CREATE UNIQUE INDEX "IX_Users_NormalizedEmail" ON "Users" ("NormalizedEmail");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261022193024_InitialCreate', '10.0.13');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119170000_AddCourses', '10.0.13');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119173000_LinkTasksToCourses', '10.0.13');

COMMIT;
