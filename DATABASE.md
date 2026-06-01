# Database Design & Connection

## Database Technology

The SideQuest platform uses **Microsoft SQL Server** as the primary database management system.

The database is implemented using:

* ASP.NET Core Identity
* Entity Framework Core
* Code First Approach
* Entity Framework Migrations
* SQL Server Database Engine

This approach allows the database structure to be managed directly through C# entity classes while maintaining version control through migrations.

---

# Database Architecture

The database is organized into the following modules:

1. Authentication & User Management
2. Company Management
3. Subscription Management
4. Categories
5. Jobs & Applications
6. Reviews & Ratings
7. Financial System
8. Gamification System
9. Skills Management
10. Notifications
11. Community System

---

# Authentication & Users

## ApplicationUser

Extends ASP.NET IdentityUser and stores platform user information.

### Fields

| Field           | Type          | Key |
| --------------- | ------------- | --- |
| Id              | string        | PK  |
| FullName        | nvarchar(100) |     |
| ProfileImageUrl | nvarchar(500) |     |
| DateOfBirth     | date          |     |
| CreatedAt       | datetime      |     |
| IsActive        | bit           |     |
| LastLoginAt     | datetime      |     |

### Roles

* Admin
* Employer
* Worker

---

# Worker Management

## WorkerProfile

Stores worker-specific information that does not belong in the main ApplicationUser table.

### Fields

| Field                | Type          | Key      |
| -------------------- | ------------- | -------- |
| Id                   | int           | PK       |
| UserId               | string        | FK       |
| Headline             | nvarchar(200) |          |
| Bio                  | nvarchar(max) |          |
| Location             | nvarchar(200) |          |
| HourlyRatePreference | decimal       | Nullable |
| AvailabilityStatus   | enum          |          |
| PortfolioUrl         | nvarchar(500) | Nullable |
| ResumeUrl            | nvarchar(500) | Nullable |
| ExperienceYears      | int           |          |
| TotalJobsCompleted   | int           |          |
| AverageRating        | decimal(3,2)  |          |
| CreatedAt            | datetime      |          |
| UpdatedAt            | datetime      |          |

### AvailabilityStatus

```text
Available
Busy
Unavailable
```

### Relationship

ApplicationUser (Worker) → WorkerProfile

Relationship Type: One-to-One

---

# Company Management

## CompanyProfile

### Fields

| Field       | Type          | Key |
| ----------- | ------------- | --- |
| Id          | int           | PK  |
| UserId      | string        | FK  |
| CompanyName | nvarchar(200) |     |
| Description | nvarchar(max) |     |
| Location    | nvarchar(200) |     |
| Website     | nvarchar(300) |     |
| LogoUrl     | nvarchar(500) |     |
| IsVerified  | bit           |     |
| VerifiedAt  | datetime      |     |
| CreatedAt   | datetime      |     |

### Relationship

ApplicationUser (Employer) → CompanyProfile

Relationship Type: One-to-One

---

# Subscription Management

## SubscriptionPlan

| Field            | Type    |
| ---------------- | ------- |
| Id               | int     |
| Name             | string  |
| Price            | decimal |
| JobLimitPerMonth | int     |
| CommissionRate   | decimal |
| Description      | string  |

### Example Plans

* Free
* Pro
* Business

---

## CompanySubscription

| Field     | Type     | Key |
| --------- | -------- | --- |
| Id        | int      | PK  |
| CompanyId | int      | FK  |
| PlanId    | int      | FK  |
| StartDate | datetime |     |
| EndDate   | datetime |     |
| IsActive  | bit      |     |

---

# Categories

## Category

| Field       | Type     |
| ----------- | -------- |
| Id          | int      |
| Name        | string   |
| Description | string   |
| IsActive    | bit      |
| CreatedAt   | datetime |

Examples:

* Sales
* Marketing
* Photography
* Event Staff
* Customer Support
* Delivery

---

# Jobs

## Job

| Field         | Type     | Key |
| ------------- | -------- | --- |
| Id            | int      | PK  |
| CompanyId     | int      | FK  |
| CategoryId    | int      | FK  |
| Title         | string   |     |
| Description   | string   |     |
| BudgetType    | enum     |     |
| FixedBudget   | decimal  |     |
| HourlyRate    | decimal  |     |
| WorkersNeeded | int      |     |
| StartDate     | datetime |     |
| EndDate       | datetime |     |
| Status        | enum     |     |
| CreatedAt     | datetime |     |

### Budget Types

* Fixed
* Hourly

### Job Status

* Draft
* Open
* InProgress
* WaitingForReview
* Completed
* Overdue
* Cancelled

---

# Job Applications

## JobApplication

| Field       | Type     | Key |
| ----------- | -------- | --- |
| Id          | int      | PK  |
| JobId       | int      | FK  |
| WorkerId    | string   | FK  |
| CoverLetter | string   |     |
| Status      | enum     |     |
| AppliedAt   | datetime |     |

### Application Status

* Pending
* Accepted
* Rejected
* Withdrawn

---

# Job Assignments

## JobAssignment

| Field       | Type     | Key |
| ----------- | -------- | --- |
| Id          | int      | PK  |
| JobId       | int      | FK  |
| WorkerId    | string   | FK  |
| AgreedRate  | decimal  |     |
| HoursWorked | decimal  |     |
| Earnings    | decimal  |     |
| IsCompleted | bit      |     |
| CompletedAt | datetime |     |

---

# Reviews & Ratings

## Review

| Field          | Type     | Key |
| -------------- | -------- | --- |
| Id             | int      | PK  |
| JobId          | int      | FK  |
| ReviewerId     | string   | FK  |
| ReviewedUserId | string   | FK  |
| Rating         | int      |     |
| Comment        | string   |     |
| CreatedAt      | datetime |     |

---

# Financial System

## BankAccount

| Field             | Type   | Key |
| ----------------- | ------ | --- |
| Id                | int    | PK  |
| UserId            | string | FK  |
| AccountHolderName | string |     |
| BankName          | string |     |
| IBAN              | string |     |
| AccountNumber     | string |     |
| IsVerified        | bit    |     |

---

## Wallet

| Field          | Type    | Key |
| -------------- | ------- | --- |
| Id             | int     | PK  |
| UserId         | string  | FK  |
| CurrentBalance | decimal |     |
| TotalEarned    | decimal |     |
| TotalWithdrawn | decimal |     |

---

## Transaction

| Field     | Type     | Key |
| --------- | -------- | --- |
| Id        | int      | PK  |
| UserId    | string   | FK  |
| JobId     | int      | FK  |
| Amount    | decimal  |     |
| Type      | enum     |     |
| Status    | enum     |     |
| CreatedAt | datetime |     |

### Transaction Types

* Earning
* Commission
* Withdrawal
* Refund

### Transaction Status

* Pending
* Completed
* Failed
* Cancelled

---

## Commission

| Field          | Type     | Key |
| -------------- | -------- | --- |
| Id             | int      | PK  |
| JobId          | int      | FK  |
| CompanyId      | int      | FK  |
| CommissionRate | decimal  |     |
| Amount         | decimal  |     |
| CreatedAt      | datetime |     |

---

# Gamification System

## UserXP

| Field  | Type   |
| ------ | ------ |
| UserId | string |
| XP     | int    |
| Level  | int    |

---

## Achievement

| Field         | Type   |
| ------------- | ------ |
| Id            | int    |
| Name          | string |
| Description   | string |
| XPRequired    | int    |
| BadgeImageUrl | string |

---

## UserAchievement

| Field         | Type     |
| ------------- | -------- |
| UserId        | string   |
| AchievementId | int      |
| EarnedAt      | datetime |

---

# Skills Management

## Skill

| Field | Type   |
| ----- | ------ |
| Id    | int    |
| Name  | string |

---

## UserSkill

| Field      | Type   |
| ---------- | ------ |
| UserId     | string |
| SkillId    | int    |
| SkillLevel | int    |

---

# Notifications

## Notification

| Field     | Type     |
| --------- | -------- |
| Id        | int      |
| UserId    | string   |
| Title     | string   |
| Message   | string   |
| Type      | string   |
| IsRead    | bit      |
| CreatedAt | datetime |

---

# Community System

## CommunityPost

| Field     | Type     |
| --------- | -------- |
| Id        | int      |
| UserId    | string   |
| Title     | string   |
| Content   | string   |
| Type      | enum     |
| CreatedAt | datetime |

---

## CommunityComment

| Field     | Type     |
| --------- | -------- |
| Id        | int      |
| PostId    | int      |
| UserId    | string   |
| Content   | string   |
| CreatedAt | datetime |

---

# Entity Framework Implementation

The project follows the Code First approach using Entity Framework Core.

Implementation steps:

1. Create Entity Models.
2. Configure DbContext.
3. Define Relationships using Fluent API.
4. Generate Initial Migration.
5. Update SQL Server Database using Migrations.
6. Maintain schema changes through future migrations.

Example commands:

```bash
Add-Migration InitialCreate
Update-Database
```

---

# Primary Relationships (ERD Summary)

ApplicationUser (1) → (1) CompanyProfile

ApplicationUser (1) → (M) JobApplication

ApplicationUser (1) → (M) JobAssignment

ApplicationUser (1) → (M) Review

ApplicationUser (1) → (M) Notification

ApplicationUser (1) → (1) Wallet

ApplicationUser (1) → (M) UserSkill

ApplicationUser (1) → (M) UserAchievement

CompanyProfile (1) → (M) Job

CompanyProfile (1) → (M) CompanySubscription

SubscriptionPlan (1) → (M) CompanySubscription

Category (1) → (M) Job

Job (1) → (M) JobApplication

Job (1) → (M) JobAssignment

Job (1) → (M) Review

Job (1) → (M) Transaction

Job (1) → (1) Commission

CommunityPost (1) → (M) CommunityComment

---

# Database Summary

Database Engine: SQL Server

ORM: Entity Framework Core

Approach: Code First

Migration Strategy: Entity Framework Migrations

Identity Management: ASP.NET Core Identity

Custom Tables: 20

This database design provides a scalable foundation for the SideQuest platform while supporting future enhancements such as advanced subscriptions, payment gateways, analytics dashboards, and additional community features.
