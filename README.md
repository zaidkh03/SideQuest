# SideQuest Platform

## Project Overview

### Project Idea

SideQuest is a web-based marketplace platform that connects employers who need short-term services with workers looking for flexible job opportunities. The platform allows employers to post jobs and hire one or multiple workers for various tasks such as marketing, sales, photography, event assistance, delivery, customer support, and other freelance or gig-based work.

### Purpose of the System

The purpose of the system is to provide a centralized platform where employers can easily find qualified workers and where workers can discover flexible earning opportunities. The platform streamlines job posting, application management, hiring, payments, reviews, and worker progression through a gamified experience.

### Main Features

#### For Workers

* User registration and authentication
* Profile management
* Skill management
* Browse available jobs
* Apply for jobs
* Track application status
* Manage active jobs
* Receive ratings and reviews
* Earn experience points (XP)
* Unlock achievements and badges
* View earnings and transaction history

#### For Employers

* Create and manage company profiles
* Post job opportunities
* Choose fixed-price or hourly-rate jobs
* Review applications
* Hire one or multiple workers
* Manage job progress
* Rate workers after job completion
* Track payments and financial transactions

#### For Administrators

* Manage users and roles
* Manage jobs and categories
* Review reported content
* Manage badges and achievements
* Monitor platform activities
* View financial statistics
* Generate reports and analytics

### Target Users

#### Workers

Individuals seeking flexible, part-time, freelance, or gig-based employment opportunities.

#### Employers

Businesses, startups, organizations, and individuals looking to hire workers for temporary or project-based tasks.

#### Administrators

Platform managers responsible for maintaining system operations, user management, and platform security.

---

## Functional Requirements

### Authentication & Authorization

* Users shall be able to register an account.
* Users shall be able to log in and log out securely.
* Users shall be able to reset forgotten passwords.
* The system shall implement role-based access control.
* The system shall support Employer, Worker, and Admin roles.
* Unauthorized users shall be restricted from protected pages.

### User Management

* Users shall be able to create and update profiles.
* Users shall be able to upload profile images.
* Users shall be able to manage personal information.
* Users shall be able to update account settings.

### Employer Functionalities

* Employers shall be able to create company profiles.
* Employers shall be able to post jobs.
* Employers shall be able to edit and delete jobs.
* Employers shall be able to select fixed-budget or hourly-rate jobs.
* Employers shall be able to receive applications.
* Employers shall be able to hire one or more workers.
* Employers shall be able to close completed jobs.
* Employers shall be able to review hired workers.

### Worker Functionalities

* Workers shall be able to browse jobs.
* Workers shall be able to search and filter jobs.
* Workers shall be able to apply for jobs.
* Workers shall be able to withdraw applications.
* Workers shall be able to track application status.
* Workers shall be able to manage active jobs.
* Workers shall be able to submit completed work.

### Job Management

* The system shall allow job creation and publishing.
* The system shall support job categories.
* The system shall support multiple job statuses.
* The system shall track job progress.
* The system shall maintain job history.

### Application Management

* Workers shall be able to submit applications.
* Employers shall be able to approve or reject applications.
* The system shall track application status changes.
* Notifications shall be generated for application updates.

### Review & Rating System

* Employers shall be able to rate workers.
* Workers shall be able to rate employers.
* The system shall calculate average ratings.
* Reviews shall be stored and displayed publicly.

### Gamification System

* Workers shall earn XP for completed jobs.
* The system shall calculate user levels.
* Workers shall unlock achievements and badges.
* The system shall display progress statistics.

### Financial Management

* The system shall maintain worker wallet balances.
* The system shall record all transactions.
* The system shall support payment tracking.
* Employers shall be able to view payment history.
* Workers shall be able to view earnings history.

### Administrative Functions

* Admins shall manage users.
* Admins shall manage job categories.
* Admins shall manage badges and achievements.
* Admins shall monitor platform activities.
* Admins shall access reports and analytics.
* Admins shall handle user complaints and reports.

### CRUD Operations

The system shall provide Create, Read, Update, and Delete operations for:

* Users
* Company Profiles
* Jobs
* Applications
* Skills
* Categories
* Reviews
* Achievements
* Badges
* Transactions

---

## Non-Functional Requirements

### Performance

* The system shall respond to user requests within acceptable time limits.
* Database queries shall be optimized for efficient execution.
* The platform shall support concurrent users without significant performance degradation.

### Security

* Passwords shall be encrypted and securely stored.
* Authentication shall use ASP.NET Identity.
* Role-based authorization shall be enforced.
* Sensitive data shall be protected from unauthorized access.
* The system shall validate all user inputs.

### Usability

* The user interface shall be intuitive and user-friendly.
* Navigation shall be consistent throughout the platform.
* Error messages shall provide meaningful feedback.
* Forms shall include validation and guidance.

### Scalability

* The system architecture shall support future growth.
* Additional modules and features shall be easily integrated.
* Database design shall support increasing data volumes.

### Responsiveness

* The platform shall support desktop, tablet, and mobile devices.
* User interfaces shall adapt to different screen sizes.

### Reliability

* The system shall maintain data consistency.
* The platform shall minimize downtime.
* Backup and recovery mechanisms shall be supported.
* Transaction records shall remain accurate and traceable.

### Maintainability

* The system shall follow clean architecture principles.
* Source code shall be modular and maintainable.
* Proper documentation shall be provided.

### Availability

* The system shall be accessible 24/7 except during maintenance periods.
* Critical services shall remain operational during normal platform usage.
