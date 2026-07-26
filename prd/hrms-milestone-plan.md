# Employee360 HRMS — Milestone Plan

## Product Vision

Employee360 HRMS is a cloud-based Human Resource Management System that centralizes employee data, automates core HR workflows, and gives employees and managers self-service tools. The goal is to replace spreadsheets and disconnected tools with one system of record for people operations.

## Target Users

| Role | Primary needs |
|------|----------------|
| **HR Admin** | Manage org structure, employee records, policies, and compliance |
| **Manager** | Approve leave, view team data, run basic reports |
| **Employee** | View profile, request leave, access documents, see payslips |
| **Executive** | Headcount, attrition, and workforce analytics (later milestones) |

## Guiding Principles

1. **Ship a usable core first** — employee records and access control before advanced modules.
2. **One source of truth** — every module reads from the same employee/org data model.
3. **Role-based by default** — permissions are enforced at API and UI layers from day one.
4. **Audit everything** — who changed what, and when, for HR-sensitive data.
5. **Mobile-friendly** — employees will use phones for leave and profile tasks.

---

## Milestone 1 — Foundation & Employee Core (MVP)

**Goal:** Establish the platform, org structure, and employee master data so HR can onboard and manage people in one place.

**Duration focus:** Platform setup + core HR data model

### Deliverables

#### 1.1 Platform & Authentication
- [ ] User registration/login (email + password; SSO deferred)
- [ ] Role-based access control (RBAC): `Super Admin`, `HR Admin`, `Manager`, `Employee`
- [ ] Password reset and session management
- [ ] Multi-tenant-ready data model (single tenant in M1, schema supports future tenants)

#### 1.2 Organization Structure
- [ ] Company profile (name, logo, address, timezone, fiscal year)
- [ ] Departments and sub-departments
- [ ] Job titles / designations
- [ ] Work locations (office, remote, hybrid)
- [ ] Reporting hierarchy (manager ↔ direct reports)

#### 1.3 Employee Master Data
- [ ] Create, view, edit, deactivate (not hard-delete) employees
- [ ] Core fields: name, employee ID, email, phone, DOB, gender, address
- [ ] Employment fields: department, designation, location, manager, join date, employment type (full-time, part-time, contract)
- [ ] Emergency contact and basic bank details (for future payroll)
- [ ] Employee status lifecycle: `Draft` → `Active` → `On Leave` → `Resigned` → `Terminated`

#### 1.4 Employee Directory & Profiles
- [ ] Searchable employee directory (filter by department, location, status)
- [ ] Employee profile page (self-view + HR edit view)
- [ ] Manager view of direct reports

#### 1.5 Admin Dashboard (Basic)
- [ ] Total headcount, active vs inactive
- [ ] New joiners this month
- [ ] Department-wise headcount chart

#### 1.6 Audit & Compliance (Basic)
- [ ] Audit log for employee record changes (who, what, when)
- [ ] Data export (CSV) for employee list

### Out of Scope for M1
- Leave, attendance, payroll, recruitment, performance reviews

### Acceptance Criteria (M1)
- HR Admin can create departments, locations, and job titles.
- HR Admin can onboard a new employee and assign manager, department, and role.
- Employee can log in and view their own profile (read-only except allowed fields).
- Manager can see their team's list and basic profiles.
- Deactivated employees cannot log in; their records remain for audit.
- All employee mutations are logged.

### Suggested Tech Stack (reference)
- **Frontend:** React/Next.js + component library (e.g. shadcn/ui)
- **Backend:** Node.js (NestJS) or Python (FastAPI)
- **Database:** PostgreSQL
- **Auth:** JWT + refresh tokens; bcrypt for passwords
- **Hosting:** Docker-ready; deploy to AWS/GCP/Vercel + managed Postgres

---

## Milestone 2 — Time, Leave & Self-Service

**Goal:** Automate the highest-frequency HR workflows — leave and attendance — and expand employee self-service.

**Depends on:** M1 employee/org data model and RBAC

### Deliverables

#### 2.1 Leave Management
- [ ] Leave types (annual, sick, unpaid, etc.) with configurable rules
- [ ] Leave policies: accrual, carry-forward, max balance, probation rules
- [ ] Employee leave application with date range and reason
- [ ] Approval workflow: Employee → Manager → HR (configurable steps)
- [ ] Leave balance tracking per employee per leave type
- [ ] Leave calendar (team view for managers)
- [ ] Email/in-app notifications for submit, approve, reject

#### 2.2 Attendance & Time Tracking
- [ ] Clock-in / clock-out (web; mobile-friendly)
- [ ] Daily attendance log per employee
- [ ] Manual attendance correction by HR (with reason + audit)
- [ ] Attendance status: Present, Absent, Half-day, On Leave, Holiday
- [ ] Company holiday calendar
- [ ] Basic attendance report (monthly summary per employee/department)

#### 2.3 Document Management
- [ ] Upload and store employee documents (offer letter, ID, contract)
- [ ] Document categories and expiry dates (e.g. work permit)
- [ ] Employee can view their own documents; HR can manage all
- [ ] File storage (S3 or equivalent) with access control

#### 2.4 Enhanced Self-Service
- [ ] Employee can request profile updates (address, phone) → HR approval
- [ ] Employee dashboard: leave balance, recent attendance, pending requests
- [ ] Manager dashboard: pending approvals, team on leave today

#### 2.5 Notifications
- [ ] In-app notification center
- [ ] Email notifications for key events (leave status, document upload, profile change request)

#### 2.6 Reporting (Operational)
- [ ] Leave utilization report
- [ ] Attendance summary report
- [ ] Export to CSV/PDF

### Out of Scope for M2
- Payroll calculation, tax, payslip generation
- Performance reviews
- Recruitment pipeline

### Acceptance Criteria (M2)
- Employee can apply for leave and see real-time balance.
- Manager receives and can approve/reject leave requests.
- Approved leave reflects on attendance and team calendar.
- Employee can clock in/out; HR can correct records with audit trail.
- Documents are uploaded securely and visible only to authorized roles.
- All approval actions trigger notifications.

---

## Milestone 3 — Payroll, Performance & Growth

**Goal:** Complete the HR lifecycle with compensation, performance management, and workforce insights.

**Depends on:** M1 employee data; M2 attendance and leave data for payroll inputs

### Deliverables

#### 3.1 Payroll Management
- [ ] Salary structure: basic, allowances, deductions
- [ ] Employee salary assignment with effective date
- [ ] Payroll run (monthly) with attendance/leave integration
- [ ] Payslip generation (PDF) per employee
- [ ] Employee self-service: view/download payslips
- [ ] Payroll summary report for HR/Finance
- [ ] Statutory deduction placeholders (tax, provident fund — configurable per region)

#### 3.2 Performance Management
- [ ] Review cycles (quarterly, annual)
- [ ] Goal setting per employee
- [ ] Self-assessment and manager review forms
- [ ] Rating scale and final rating
- [ ] Review history on employee profile

#### 3.3 Recruitment (Basic ATS)
- [ ] Job posting creation and publish (internal or public link)
- [ ] Applicant tracking: Applied → Screening → Interview → Offer → Hired/Rejected
- [ ] Candidate profile and resume upload
- [ ] Convert hired candidate to employee (pre-fill onboarding)

#### 3.4 Onboarding & Offboarding Workflows
- [ ] Onboarding checklist (IT setup, documents, orientation tasks)
- [ ] Offboarding checklist (asset return, access revocation, exit interview)
- [ ] Task assignment to HR, IT, and manager

#### 3.5 Analytics & Executive Dashboard
- [ ] Headcount trends over time
- [ ] Attrition rate and turnover report
- [ ] Leave and attendance trends
- [ ] Department-wise cost summary (from payroll)
- [ ] Custom date-range filters and export

#### 3.6 Advanced Admin
- [ ] Custom fields on employee profile (configurable by HR)
- [ ] Bulk import employees (CSV)
- [ ] API webhooks or basic REST API for integrations (optional)
- [ ] System settings: branding, email templates, notification preferences

### Out of Scope for M3 (Future Roadmap)
- Full accounting/ERP integration
- Learning management (LMS)
- Advanced workforce planning / succession
- Mobile native apps
- SSO / SAML (can be added earlier if needed)

### Acceptance Criteria (M3)
- HR can run monthly payroll and generate payslips for all active employees.
- Payslips reflect attendance and unpaid leave deductions where configured.
- Managers and employees can complete a performance review cycle end-to-end.
- HR can post a job, track candidates, and convert a hire to an employee record.
- Onboarding/offboarding checklists can be created and tracked to completion.
- Executive dashboard shows accurate headcount and attrition metrics.

---

## Cross-Milestone Requirements

These apply across all milestones:

| Area | Requirement |
|------|-------------|
| **Security** | HTTPS, hashed passwords, RBAC on every endpoint, input validation |
| **Privacy** | PII encrypted at rest where feasible; role-scoped data access |
| **Audit** | Immutable audit log for sensitive operations |
| **UX** | Responsive layout; loading, empty, and error states on all screens |
| **Testing** | Unit tests for business logic; integration tests for critical flows |
| **Documentation** | API docs, admin setup guide, deployment README |

---

## Data Model (High-Level)

```
Company
  └── Department
  └── Location
  └── JobTitle
  └── Employee
        ├── User (auth)
        ├── Manager (self-ref)
        ├── LeaveBalance / LeaveRequest (M2)
        ├── AttendanceRecord (M2)
        ├── Document (M2)
        ├── SalaryStructure / Payslip (M3)
        ├── PerformanceReview (M3)
        └── OnboardingTask (M3)

JobPosting → Candidate (M3)
```

---

## Milestone Summary

| Milestone | Theme | Key outcome |
|-----------|--------|-------------|
| **M1** | Foundation & Employee Core | HR can manage org + employees in one system |
| **M2** | Time, Leave & Self-Service | Daily HR ops (leave + attendance) are automated |
| **M3** | Payroll, Performance & Growth | Full employee lifecycle + workforce insights |

---

## Recommended Build Order (M1 Internal Sprints)

Even within M1, ship in this order:

1. **Sprint 1:** Project scaffold, DB schema, auth, RBAC
2. **Sprint 2:** Org structure CRUD (departments, locations, titles)
3. **Sprint 3:** Employee CRUD + status lifecycle + audit log
4. **Sprint 4:** Directory, profiles, manager views, admin dashboard
5. **Sprint 5:** Polish, testing, deployment, documentation

---

## Open Questions (Decide Before Build)

1. **Single company or multi-tenant SaaS?** (affects auth and data isolation)
2. **Target geography?** (affects payroll statutory rules, date formats, compliance)
3. **Company size?** (SMB < 200 employees vs mid-market changes reporting needs)
4. **Attendance model?** (simple clock-in vs shift-based vs biometric integration)
5. **Payroll complexity?** (payslip display only vs full tax filing integration)
6. **Existing tools to integrate?** (Google Workspace, Slack, accounting software)

---

## Success Metrics

| Milestone | Metric |
|-----------|--------|
| M1 | HR can onboard 100% of employees digitally; zero parallel spreadsheets |
| M2 | >80% of leave requests submitted and approved in-system |
| M3 | Monthly payroll completed in-system; review cycle completion rate >90% |
