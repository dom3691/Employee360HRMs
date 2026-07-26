#!/usr/bin/env python3
"""Generate Employee360 HRMS comprehensive PRD as Word document."""

from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
import datetime

OUTPUT = "/workspace/docs/Employee360-HRMS-PRD.docx"


def set_cell_shading(cell, color_hex):
    shading = OxmlElement("w:shd")
    shading.set(qn("w:fill"), color_hex)
    cell._tc.get_or_add_tcPr().append(shading)


def add_heading(doc, text, level=1):
    return doc.add_heading(text, level=level)


def add_para(doc, text, bold=False, italic=False):
    p = doc.add_paragraph()
    run = p.add_run(text)
    run.bold = bold
    run.italic = italic
    return p


def add_table(doc, headers, rows, header_color="1F4E79"):
    table = doc.add_table(rows=1 + len(rows), cols=len(headers))
    table.style = "Table Grid"
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    hdr = table.rows[0].cells
    for i, h in enumerate(headers):
        hdr[i].text = h
        set_cell_shading(hdr[i], header_color)
        for p in hdr[i].paragraphs:
            for r in p.runs:
                r.bold = True
                r.font.color.rgb = RGBColor(255, 255, 255)
                r.font.size = Pt(9)
    for ri, row in enumerate(rows):
        cells = table.rows[ri + 1].cells
        for ci, val in enumerate(row):
            cells[ci].text = str(val)
            for p in cells[ci].paragraphs:
                for r in p.runs:
                    r.font.size = Pt(9)
    doc.add_paragraph()
    return table


def add_user_story(doc, role, action, benefit, criteria):
    add_para(doc, f"As a {role}, I want {action}, so that {benefit}.", bold=True)
    add_para(doc, "Acceptance Criteria:", bold=True)
    for c in criteria:
        doc.add_paragraph(c, style="List Bullet")


def add_fr(doc, fr_id, module, requirement, priority, notes=""):
    # stored in bulk table later
    pass


def build_document():
    doc = Document()
    style = doc.styles["Normal"]
    style.font.name = "Calibri"
    style.font.size = Pt(11)

    # Title page
    title = doc.add_paragraph()
    title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = title.add_run("Employee360 HRMS")
    run.bold = True
    run.font.size = Pt(28)
    run.font.color.rgb = RGBColor(31, 78, 121)

    sub = doc.add_paragraph()
    sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r2 = sub.add_run("Product Requirements Document (PRD)")
    r2.font.size = Pt(16)
    r2.italic = True

    sub2 = doc.add_paragraph()
    sub2.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r3 = sub2.add_run("Enterprise Human Resource Management System\nFor Nigerian Organizations")
    r3.font.size = Pt(12)

    doc.add_paragraph()
    meta = doc.add_paragraph()
    meta.alignment = WD_ALIGN_PARAGRAPH.CENTER
    meta.add_run(f"Document Version: 1.0\nDate: {datetime.date.today().strftime('%d %B %Y')}\nClassification: Internal — Confidential")

    doc.add_page_break()

    # Table of Contents placeholder
    add_heading(doc, "Table of Contents", 1)
    toc_items = [
        "1. Document Control", "2. Executive Summary", "3. Problem Statement & Current-State Pain Points",
        "4. Goals, Objectives & Success Metrics", "5. Scope", "6. User Personas & Roles",
        "7. User Stories & Use Cases", "8. Functional Requirements", "9. Non-Functional Requirements",
        "10. Workflow & Approval Logic", "11. Data Model Overview", "12. Integration Requirements",
        "13. Security & Compliance", "14. Reporting & Analytics Requirements",
        "15. Technical Architecture Overview", "16. Assumptions, Constraints & Dependencies",
        "17. Release Plan / Phased Roadmap", "18. Risks & Mitigations", "19. Glossary of Terms",
        "20. Appendix"
    ]
    for item in toc_items:
        doc.add_paragraph(item, style="List Number")
    doc.add_page_break()

    # 1. Document Control
    add_heading(doc, "1. Document Control", 1)
    add_table(doc,
        ["Field", "Value"],
        [
            ["Document Title", "Employee360 HRMS — Product Requirements Document"],
            ["Document ID", "PRD-E360-001"],
            ["Version", "1.0"],
            ["Status", "Draft — For Review"],
            ["Author", "Product Management / Solutions Architecture"],
            ["Creation Date", datetime.date.today().strftime("%d %B %Y")],
            ["Last Updated", datetime.date.today().strftime("%d %B %Y")],
            ["Product Name", "Employee360 HRMS"],
            ["Target Market", "Nigerian Enterprises (50–5,000+ employees)"],
            ["Distribution", "Internal — Engineering, QA, HR, IT, Executive Stakeholders"],
        ])
    add_para(doc, "Revision History", bold=True)
    add_table(doc,
        ["Version", "Date", "Author", "Description"],
        [
            ["0.1", "2026-07-01", "Product Management", "Initial outline and stakeholder interviews"],
            ["0.5", "2026-07-15", "Solutions Architecture", "Technical architecture and data model draft"],
            ["1.0", datetime.date.today().strftime("%d %b %Y"), "Product Management", "Full PRD for Phase 1–3 delivery"],
        ])
    add_para(doc, "Approvers", bold=True)
    add_table(doc,
        ["Name", "Role", "Signature", "Date", "Status"],
        [
            ["[TBD]", "Head of HR", "", "", "Pending"],
            ["[TBD]", "Chief Information Officer", "", "", "Pending"],
            ["[TBD]", "Chief Financial Officer", "", "", "Pending"],
            ["[TBD]", "Engineering Lead", "", "", "Pending"],
            ["[TBD]", "Data Protection Officer", "", "", "Pending"],
        ])

    # 2. Executive Summary
    add_heading(doc, "2. Executive Summary", 1)
    add_para(doc,
        "Employee360 HRMS is a custom-built, enterprise-grade Human Resource Management System designed "
        "specifically for organizations operating in Nigeria. The platform replaces fragmented spreadsheets, "
        "paper-based workflows, and disconnected point solutions with a unified digital system covering the "
        "full employee lifecycle — from hire to retire.")
    add_para(doc,
        "The system will be delivered in three phased releases. Phase 1 (MVP) establishes the employee "
        "master data foundation, role-based security, leave management, and employee/manager self-service "
        "portals. Phase 2 adds attendance and time management, Nigeria-compliant payroll (PAYE, Pension, "
        "NHF, NSITF), and operational analytics. Phase 3 extends the platform with recruitment, onboarding, "
        "and performance management capabilities.")
    add_para(doc,
        "Built on Microsoft technologies — ASP.NET Core, Angular, SQL Server, and Azure — Employee360 "
        "aligns with enterprise IT standards, supports future Azure AD/SSO integration, and is architected "
        "for scalability, auditability, and regulatory compliance under the Nigeria Data Protection "
        "Regulation (NDPR).")
    add_para(doc, "Key Business Outcomes:", bold=True)
    outcomes = [
        "Reduce HR administrative processing time by 60% within 12 months of Phase 1 go-live.",
        "Achieve 95%+ employee adoption of self-service for leave requests within 6 months.",
        "Eliminate parallel payroll spreadsheets and reduce payroll processing cycle from days to hours.",
        "Provide real-time workforce visibility to HR leadership and line managers.",
        "Establish a compliant, auditable system of record for all employee and payroll data.",
    ]
    for o in outcomes:
        doc.add_paragraph(o, style="List Bullet")

    # 3. Problem Statement
    add_heading(doc, "3. Problem Statement & Current-State Pain Points", 1)
    add_para(doc,
        "Nigerian enterprises — particularly those with 50+ employees across multiple locations — face "
        "significant operational friction in managing human resources. Current-state processes are "
        "predominantly manual, decentralized, and error-prone, creating compliance risk, employee "
        "dissatisfaction, and inability to make data-driven workforce decisions.")
    add_para(doc, "Current-State Pain Points", bold=True)
    add_table(doc,
        ["Pain Point", "Description", "Business Impact"],
        [
            ["Fragmented employee data", "Employee records maintained across Excel, email, and paper files with no single source of truth.", "Data inconsistency; inability to produce accurate headcount reports; onboarding delays."],
            ["Manual leave management", "Leave requests via email/WhatsApp; balances tracked in spreadsheets; no audit trail.", "Policy violations; payroll errors from unrecorded leave; manager bottlenecks."],
            ["Payroll complexity", "Nigerian statutory deductions (PAYE, Pension 8%/10%, NHF 2.5%, NSITF) calculated manually.", "Compliance risk with FIRS/LASRRA; calculation errors; delayed salary payments."],
            ["No self-service", "Employees depend on HR for profile updates, leave balances, payslips.", "HR team overwhelmed; poor employee experience; low transparency."],
            ["Attendance gaps", "No centralized time tracking; biometric devices not integrated.", "Inaccurate payroll inputs; inability to enforce shift policies."],
            ["Recruitment disconnection", "Hiring tracked separately from employee records.", "Duplicate data entry; poor candidate experience; slow time-to-hire."],
            ["Limited reporting", "No real-time dashboards; reports require manual compilation.", "Delayed decisions; inability to track attrition, leave trends, or payroll costs."],
            ["Compliance exposure", "Inadequate audit trails; PII stored insecurely; NDPR obligations unmet.", "Regulatory penalties; reputational risk; data breach exposure."],
            ["Scalability constraints", "Current tools do not scale with headcount growth or multi-location operations.", "Process breakdown as organization grows; increased HR headcount needed."],
        ])

    # 4. Goals, Objectives & Success Metrics
    add_heading(doc, "4. Goals, Objectives & Success Metrics", 1)
    add_para(doc, "Strategic Goals", bold=True)
    add_table(doc,
        ["Goal ID", "Goal", "Description"],
        [
            ["G-01", "Unified HR Platform", "Establish Employee360 as the single system of record for all employee and HR transactional data."],
            ["G-02", "Regulatory Compliance", "Ensure payroll and data handling comply with Nigerian tax, pension, and data protection regulations."],
            ["G-03", "Employee Empowerment", "Enable employees and managers to complete routine HR tasks without HR intervention."],
            ["G-04", "Operational Efficiency", "Automate approval workflows, calculations, and notifications to reduce manual processing."],
            ["G-05", "Workforce Intelligence", "Provide leadership with accurate, timely analytics on headcount, attrition, leave, and payroll costs."],
        ])
    add_para(doc, "Objectives & Measurable KPIs", bold=True)
    add_table(doc,
        ["Objective", "KPI", "Baseline", "Target", "Measurement Method", "Timeline"],
        [
            ["Reduce HR admin burden", "Avg. time to process leave request", "3–5 days", "< 24 hours", "System timestamp: submit → final approval", "6 months post Phase 1"],
            ["Employee adoption", "% leave requests via self-service", "< 20%", "≥ 95%", "System transaction logs", "6 months post Phase 1"],
            ["Data accuracy", "Employee record error rate", "Unknown (~15% est.)", "< 2%", "Quarterly data audit sample", "12 months post Phase 1"],
            ["Payroll accuracy", "Payroll adjustment rate per cycle", "> 10%", "< 2%", "Payroll correction count / total payslips", "3 months post Phase 2"],
            ["Payroll cycle time", "Days from cut-off to bank file generation", "5–7 days", "≤ 2 days", "Payroll run timestamps", "3 months post Phase 2"],
            ["Attendance compliance", "% attendance records captured digitally", "< 30%", "≥ 90%", "Attendance records / expected records", "6 months post Phase 2"],
            ["Manager engagement", "% approvals completed within SLA", "N/A", "≥ 85%", "Approval workflow timestamps", "6 months post Phase 1"],
            ["System availability", "Uptime during business hours", "N/A", "≥ 99.5%", "Azure Monitor / Application Insights", "Ongoing"],
            ["User satisfaction", "Net Promoter Score (employees)", "N/A", "≥ 40", "Quarterly pulse survey", "12 months post Phase 1"],
            ["Time-to-hire", "Days from job posting to offer acceptance", "45+ days", "≤ 30 days", "Recruitment module timestamps", "6 months post Phase 3"],
        ])

    # 5. Scope
    add_heading(doc, "5. Scope", 1)
    add_para(doc, "5.1 In-Scope — Phase 1 (MVP)", bold=True)
    phase1_in = [
        "Employee Management: core master data, profiles, documents, org chart, departments, positions",
        "Authentication, Roles & Permissions (RBAC) with JWT",
        "Leave Management: configurable leave types, approval workflows, balances and accrual",
        "Employee Self-Service (ESS) and Manager Self-Service (MSS) dashboards",
        "Email notifications (SMTP) for key workflow events",
        "Audit logging for sensitive data changes",
        "Basic admin configuration (company profile, working calendar, public holidays)",
    ]
    for item in phase1_in:
        doc.add_paragraph(item, style="List Bullet")

    add_para(doc, "5.2 In-Scope — Phase 2", bold=True)
    phase2_in = [
        "Attendance & Time Management: clock-in/out, shifts, timesheets, device integration readiness",
        "Nigeria-specific Payroll: PAYE, Pension (8% employee / 10% employer), NHF, NSITF, salary structures, deductions, payslips, bank payment files",
        "Reports & Analytics: headcount, attrition, leave trends, payroll cost dashboards",
        "Payroll approval workflow and payroll period locking",
    ]
    for item in phase2_in:
        doc.add_paragraph(item, style="List Bullet")

    add_para(doc, "5.3 In-Scope — Phase 3", bold=True)
    phase3_in = [
        "Recruitment & Onboarding: job postings, applicant tracking, offer letters, onboarding checklists",
        "Performance Management: goals/KPIs, appraisals, 360-degree reviews",
    ]
    for item in phase3_in:
        doc.add_paragraph(item, style="List Bullet")

    add_para(doc, "5.4 Out-of-Scope (All Phases / Later Roadmap)", bold=True)
    add_table(doc,
        ["Item", "Rationale", "Planned Phase"],
        [
            ["Training & Development (LMS)", "Requires separate content management; lower priority than core HR.", "Later"],
            ["Benefits & Compliance module", "Complex benefits administration deferred until payroll stable.", "Later"],
            ["Native mobile applications (iOS/Android)", "Responsive web meets MVP; native apps evaluated post-adoption.", "Later"],
            ["Full ERP/Accounting integration", "Bank payment file export provided; full GL integration is future.", "Later"],
            ["Multi-country payroll", "Nigeria-only in initial releases.", "Later"],
            ["Biometric hardware procurement", "Integration APIs provided; hardware supplied by client.", "Phase 2 (integration only)"],
            ["Azure AD / SSO implementation", "Architecture supports; implementation deferred.", "Post Phase 1"],
            ["Employee wellness / engagement surveys", "Not core HRMS scope.", "Later"],
            ["Union/Collective bargaining management", "Client-specific; requires separate analysis.", "TBD"],
        ])

    # 6. User Personas & Roles
    add_heading(doc, "6. User Personas & Roles", 1)
    add_table(doc,
        ["Persona", "Description", "Primary Goals", "Key Permissions"],
        [
            ["HR Admin", "Day-to-day HR operations staff managing employee records, leave policies, and system configuration.", "Maintain accurate employee data; configure leave policies; support employees; generate reports.", "Full CRUD on employee records; configure leave types/policies; view all employee data; manage documents; run HR reports."],
            ["HR Manager", "Head of HR / HR Business Partner with oversight of HR operations and policy.", "Ensure policy compliance; approve escalated requests; monitor workforce metrics.", "All HR Admin permissions plus: approve escalated leave; view analytics dashboards; approve profile changes; lock payroll periods (Phase 2)."],
            ["Line Manager", "Department head or team lead with direct reports.", "Manage team leave approvals; view team attendance; conduct performance reviews (Phase 3).", "View direct/indirect reports; approve/reject leave; view team calendar; view team attendance (Phase 2); complete appraisals (Phase 3)."],
            ["Employee", "All staff members using self-service.", "View/update personal info; request leave; access payslips; view attendance.", "View own profile; submit leave requests; view leave balance; download payslips (Phase 2); clock in/out (Phase 2); view own documents."],
            ["Payroll Officer", "Finance/HR staff responsible for salary processing.", "Run accurate, compliant payroll; generate bank files and payslips.", "Manage salary structures; run payroll; generate payslips; export bank payment files; view payroll reports. No access to recruitment (unless dual-role)."],
            ["System/IT Admin", "Technical administrator managing users, roles, system settings.", "Maintain system security; manage user accounts; configure integrations.", "Manage roles/permissions; system configuration; view audit logs; manage integrations; no access to employee PII unless granted."],
            ["Executive / Viewer", "C-suite or senior leadership (read-only).", "Monitor workforce KPIs and trends.", "View executive dashboards and reports only; no transactional access."],
        ])

    add_para(doc, "Role Hierarchy & Inheritance", bold=True)
    add_para(doc,
        "Roles are flat assignments (a user may hold multiple roles). Permissions are additive. "
        "Manager permissions are scoped to direct and indirect reports via the reporting hierarchy. "
        "Department-scoped data access may be configured for HR Business Partners in a future enhancement.")

    # 7. User Stories
    add_heading(doc, "7. User Stories & Use Cases", 1)

    add_heading(doc, "7.1 Authentication & RBAC", 2)
    add_user_story(doc, "Employee", "to log in securely with my corporate email and password",
        "I can access my self-service portal",
        ["Given I am a registered active employee, When I enter valid credentials, Then I am authenticated and redirected to my ESS dashboard.",
         "Given I enter invalid credentials 5 times, When I attempt a 6th login, Then my account is temporarily locked for 15 minutes.",
         "Given my account is deactivated, When I attempt to log in, Then I receive an error and cannot access the system.",
         "Given I am authenticated, When my JWT expires, Then I am prompted to re-authenticate or use refresh token."])
    add_user_story(doc, "System Admin", "to assign roles and permissions to users",
        "access is controlled according to job function",
        ["Given I am a System Admin, When I assign the 'Line Manager' role to a user, Then that user can view and approve leave for their direct reports only.",
         "Given I remove a role from a user, When they next authenticate, Then their permissions reflect the updated role assignment.",
         "Given a permission change is made, When the change is saved, Then an audit log entry is created with actor, timestamp, and change details."])

    add_heading(doc, "7.2 Employee Management", 2)
    add_user_story(doc, "HR Admin", "to create and maintain employee master records",
        "the organization has a single source of truth for employee data",
        ["Given I am an HR Admin, When I create a new employee with required fields, Then the employee record is saved with status 'Active' and a unique Employee ID is generated.",
         "Given an employee exists, When I update their department or manager, Then the org chart and reporting relationships are updated immediately.",
         "Given I deactivate an employee, When I set status to 'Terminated', Then the employee cannot log in but their historical records are retained.",
         "Given I upload a document to an employee profile, When the upload completes, Then the document is stored securely and visible per RBAC rules."])
    add_user_story(doc, "Employee", "to view my profile and employment details",
        "I can verify my information is correct",
        ["Given I am logged in, When I navigate to My Profile, Then I see my personal details, employment info, manager, department, and job title.",
         "Given I request a profile change (e.g., phone number), When I submit the request, Then it is routed to HR for approval and I receive a notification on decision."])
    add_user_story(doc, "Line Manager", "to view my team's org structure and profiles",
        "I can understand my reporting structure and team composition",
        ["Given I am a Line Manager, When I open the Team view, Then I see all direct reports with name, title, department, and status.",
         "Given I view the Org Chart, When I click on a team member, Then I see their basic profile (excluding sensitive fields like bank details)."])

    add_heading(doc, "7.3 Leave Management", 2)
    add_user_story(doc, "Employee", "to apply for leave and view my balance",
        "I can plan time off without emailing HR",
        ["Given I have 10 days annual leave balance, When I apply for 3 days leave, Then my pending balance shows 7 days upon submission.",
         "Given my leave request overlaps a public holiday, When I submit the request, Then public holidays are excluded from working days calculation.",
         "Given my manager approves my leave, When I view my requests, Then status shows 'Approved' and I receive an email notification.",
         "Given my leave exceeds available balance, When I submit the request, Then the system prevents submission and displays an error."])
    add_user_story(doc, "Line Manager", "to approve or reject leave requests from my team",
        "team coverage is managed efficiently",
        ["Given a direct report submits leave, When I open my approval queue, Then I see the request with dates, type, balance, and reason.",
         "Given I approve a request, When I confirm, Then the employee is notified and the leave is reflected on the team calendar.",
         "Given I do not act within 48 hours, When the SLA expires, Then the request is escalated to the next-level manager or HR."])

    add_heading(doc, "7.4 Attendance & Time (Phase 2)", 2)
    add_user_story(doc, "Employee", "to clock in and clock out",
        "my attendance is recorded accurately for payroll",
        ["Given I am within an allowed location/shift window, When I clock in, Then an attendance record is created with timestamp and source (web/device).",
         "Given I forgot to clock out, When I notify my manager, Then the manager or HR can submit a correction with reason (audit logged)."])
    add_user_story(doc, "HR Admin", "to configure shifts and work schedules",
        "attendance rules align with company policy",
        ["Given I define a shift (08:00–17:00, Mon–Fri), When assigned to a department, Then employees in that department are evaluated against that schedule.",
         "Given an employee clocks in 30 minutes late, When the daily record is processed, Then status reflects 'Late' per configured grace period."])

    add_heading(doc, "7.5 Payroll (Phase 2)", 2)
    add_user_story(doc, "Payroll Officer", "to run monthly payroll with Nigerian statutory deductions",
        "employees are paid accurately and compliantly",
        ["Given an approved payroll period, When I initiate a payroll run, Then PAYE, Pension (8%/10%), NHF (2.5%), and NSITF are calculated per configured rules.",
         "Given payroll is calculated, When I review the summary, Then I can see gross pay, each deduction line item, and net pay per employee.",
         "Given payroll is approved, When I generate payslips, Then each employee can download their PDF payslip from self-service.",
         "Given payroll is finalized, When I export the bank payment file, Then it is formatted per configured bank template (e.g., NIBSS/NEFT CSV)."])
    add_user_story(doc, "Employee", "to view and download my payslips",
        "I have transparent access to my compensation details",
        ["Given payroll has been finalized for a period, When I navigate to My Payslips, Then I see a list of available payslips by month.",
         "Given I select a payslip, When I download it, Then I receive a PDF showing earnings, statutory deductions, and net pay in NGN (₦)."])

    add_heading(doc, "7.6 Recruitment & Onboarding (Phase 3)", 2)
    add_user_story(doc, "HR Admin", "to post job openings and track applicants",
        "hiring is managed within the same platform as employee records",
        ["Given I create a job posting, When I publish it, Then it is visible on the internal/external careers page.",
         "Given a candidate applies, When I update their stage to 'Interview', Then the hiring team is notified.",
         "Given a candidate is marked 'Hired', When I initiate onboarding, Then a new employee record is pre-populated from candidate data."])

    add_heading(doc, "7.7 Performance Management (Phase 3)", 2)
    add_user_story(doc, "Line Manager", "to set goals and conduct appraisals for my team",
        "performance is tracked and documented systematically",
        ["Given a review cycle is active, When I set KPIs for a direct report, Then the employee can view their goals in self-service.",
         "Given the employee completes self-assessment, When I submit my manager review, Then the final rating is calculated per configured weighting.",
         "Given a 360 review is configured, When peer feedback is submitted, Then it is aggregated anonymously in the review summary."])

    add_heading(doc, "7.8 Employee & Manager Self-Service (ESS/MSS)", 2)
    add_user_story(doc, "Employee", "to see a personalized dashboard when I log in",
        "I have immediate visibility of my HR tasks and status",
        ["Given I log in as an Employee, When the ESS dashboard loads, Then I see widgets for leave balance, pending requests, notifications, and quick actions.",
         "Given I have no pending items, When I view the dashboard, Then I see an empty state with guidance to apply for leave or update my profile."])
    add_user_story(doc, "Line Manager", "to see all pending approvals in one place",
        "I can action team requests efficiently without searching emails",
        ["Given I have 3 pending leave approvals, When I open MSS Approvals, Then I see all 3 with employee name, dates, type, and days requested.",
         "Given I bulk-approve eligible requests, When I select multiple and confirm, Then each is processed individually with audit entries."])

    add_heading(doc, "7.9 Administration & Configuration", 2)
    add_user_story(doc, "HR Admin", "to configure company public holidays",
        "leave calculations exclude non-working days correctly",
        ["Given I add a public holiday for 1 October (Independence Day), When an employee applies leave spanning that date, Then 1 October is excluded from working days.",
         "Given I import holidays for the full year, When saved, Then they appear on the leave calendar for all users."])
    add_user_story(doc, "System Admin", "to view system audit logs",
        "security incidents and data changes can be investigated",
        ["Given I filter audit logs by employee entity and date range, When results load, Then I see all create/update/delete actions with actor, timestamp, and field-level changes.",
         "Given I export audit logs, When I click Export, Then I receive a CSV file for the filtered period."])

    add_heading(doc, "7.10 Use Cases (Narrative)", 2)
    add_para(doc, "UC-001: Onboard New Employee", bold=True)
    add_para(doc,
        "Primary Actor: HR Admin. Precondition: Departments and positions exist. "
        "Flow: HR Admin creates employee record with personal and employment details → System generates Employee ID → "
        "System creates user account and sends welcome email with password setup link → Employee logs in and completes profile → "
        "HR uploads onboarding documents. Postcondition: Employee appears in directory with Active status.")
    add_para(doc, "UC-002: Monthly Payroll Processing", bold=True)
    add_para(doc,
        "Primary Actor: Payroll Officer. Precondition: Attendance and leave data finalized for period; salary structures assigned. "
        "Flow: Payroll Officer initiates payroll run → System calculates gross, PAYE, Pension, NHF, NSITF, and custom deductions → "
        "Payroll Officer reviews exceptions → Submits for HR Manager approval → Finance approves → "
        "System generates payslips and bank file → Period locked. Postcondition: Employees can access payslips; bank file ready for upload.")
    add_para(doc, "UC-003: Employee Resignation & Offboarding", bold=True)
    add_para(doc,
        "Primary Actor: HR Admin. Flow: HR Admin sets employee status to Resigned with last working day → "
        "System revokes login access on last working day → Leave balance finalized → "
        "Offboarding checklist initiated (Phase 3) → Employee record retained for audit. Postcondition: Employee excluded from active payroll and headcount.")

    # 8. Functional Requirements
    add_heading(doc, "8. Functional Requirements", 1)
    add_para(doc, "Priority Legend: Must = mandatory for release; Should = important but can defer within phase; Could = desirable; Won't = explicitly excluded this phase.", italic=True)

    fr_data = [
        # AUTH
        ["FR-AUTH-001", "Authentication", "System shall support email + password login with JWT access tokens and refresh tokens.", "Must", "Phase 1"],
        ["FR-AUTH-002", "Authentication", "System shall enforce password policy: min 8 chars, uppercase, lowercase, number, special character.", "Must", "Phase 1"],
        ["FR-AUTH-003", "Authentication", "System shall lock accounts after 5 failed login attempts for 15 minutes.", "Must", "Phase 1"],
        ["FR-AUTH-004", "Authentication", "System shall support password reset via email token (expires in 1 hour).", "Must", "Phase 1"],
        ["FR-AUTH-005", "Authentication", "System architecture shall support future Azure AD/OIDC SSO integration without schema changes.", "Must", "Phase 1"],
        ["FR-AUTH-006", "RBAC", "System shall support role-based permissions configurable per module and action (CRUD).", "Must", "Phase 1"],
        ["FR-AUTH-007", "RBAC", "System shall support multi-role assignment per user with additive permissions.", "Must", "Phase 1"],
        ["FR-AUTH-008", "RBAC", "Manager-scoped data access shall be enforced based on reporting hierarchy.", "Must", "Phase 1"],
        # EMP
        ["FR-EMP-001", "Employee Mgmt", "System shall auto-generate unique Employee ID on creation (configurable format, e.g., EMP-00001).", "Must", "Phase 1"],
        ["FR-EMP-002", "Employee Mgmt", "System shall store core personal data: name, DOB, gender, nationality, marital status, contact details, address, NIN (optional).", "Must", "Phase 1"],
        ["FR-EMP-003", "Employee Mgmt", "System shall store employment data: department, position, grade, manager, join date, employment type, work location.", "Must", "Phase 1"],
        ["FR-EMP-004", "Employee Mgmt", "System shall support employee status lifecycle: Draft, Active, On Leave, Suspended, Resigned, Terminated.", "Must", "Phase 1"],
        ["FR-EMP-005", "Employee Mgmt", "System shall soft-delete employee records (IsDeleted flag); no hard delete of employee data.", "Must", "Phase 1"],
        ["FR-EMP-006", "Employee Mgmt", "System shall maintain audit columns: CreatedBy, CreatedAt, ModifiedBy, ModifiedAt on all entities.", "Must", "Phase 1"],
        ["FR-EMP-007", "Employee Mgmt", "System shall support employee document upload (PDF, JPG, PNG; max 10MB) with category tagging.", "Must", "Phase 1"],
        ["FR-EMP-008", "Employee Mgmt", "System shall display interactive org chart based on reporting hierarchy.", "Should", "Phase 1"],
        ["FR-EMP-009", "Employee Mgmt", "System shall support department CRUD with parent-child hierarchy.", "Must", "Phase 1"],
        ["FR-EMP-010", "Employee Mgmt", "System shall support position/job title CRUD linked to departments and grades.", "Must", "Phase 1"],
        ["FR-EMP-011", "Employee Mgmt", "System shall support employee profile change requests with HR approval workflow.", "Should", "Phase 1"],
        ["FR-EMP-012", "Employee Mgmt", "System shall support bulk employee import via CSV template.", "Could", "Phase 1"],
        ["FR-EMP-013", "Employee Mgmt", "System shall store emergency contact and next-of-kin details.", "Must", "Phase 1"],
        ["FR-EMP-014", "Employee Mgmt", "System shall store bank account details (bank name, account number, account name) for payroll.", "Must", "Phase 1"],
        # LEAVE
        ["FR-LV-001", "Leave Mgmt", "System shall support configurable leave types (Annual, Sick, Maternity, Paternity, Compassionate, Unpaid).", "Must", "Phase 1"],
        ["FR-LV-002", "Leave Mgmt", "System shall support leave policy configuration: annual entitlement, accrual frequency, carry-forward rules, max carry-forward, probation exclusion.", "Must", "Phase 1"],
        ["FR-LV-003", "Leave Mgmt", "System shall calculate working days excluding weekends and configured public holidays.", "Must", "Phase 1"],
        ["FR-LV-004", "Leave Mgmt", "System shall track leave balance per employee per leave type per year.", "Must", "Phase 1"],
        ["FR-LV-005", "Leave Mgmt", "System shall support leave application with start date, end date, type, reason, and attachment (optional).", "Must", "Phase 1"],
        ["FR-LV-006", "Leave Mgmt", "System shall enforce leave balance validation before submission.", "Must", "Phase 1"],
        ["FR-LV-007", "Leave Mgmt", "System shall route leave requests through configurable approval chain: Employee → Line Manager → HR (optional).", "Must", "Phase 1"],
        ["FR-LV-008", "Leave Mgmt", "System shall support approve, reject, and return-for-revision actions with comments.", "Must", "Phase 1"],
        ["FR-LV-009", "Leave Mgmt", "System shall auto-escalate pending approvals after configurable SLA (default 48 hours).", "Should", "Phase 1"],
        ["FR-LV-010", "Leave Mgmt", "System shall display team leave calendar for managers.", "Must", "Phase 1"],
        ["FR-LV-011", "Leave Mgmt", "System shall send email notifications on leave submit, approve, reject, and escalate.", "Must", "Phase 1"],
        ["FR-LV-012", "Leave Mgmt", "HR Admin shall be able to manually adjust leave balances with reason (audit logged).", "Must", "Phase 1"],
        # ESS/MSS
        ["FR-ESS-001", "Self-Service", "ESS dashboard shall show leave balance, pending requests, and recent notifications.", "Must", "Phase 1"],
        ["FR-ESS-002", "Self-Service", "ESS shall provide quick actions: Apply Leave, View Profile, View Documents.", "Must", "Phase 1"],
        ["FR-MSS-001", "Manager Self-Service", "MSS dashboard shall show pending approvals, team on leave today, and team headcount.", "Must", "Phase 1"],
        ["FR-MSS-002", "Manager Self-Service", "MSS shall provide team list with search and filter by status.", "Must", "Phase 1"],
        # ATTENDANCE
        ["FR-ATT-001", "Attendance", "System shall support web-based clock-in/clock-out with timestamp and IP/device logging.", "Must", "Phase 2"],
        ["FR-ATT-002", "Attendance", "System shall support shift definitions (start, end, grace period, break duration).", "Must", "Phase 2"],
        ["FR-ATT-003", "Attendance", "System shall calculate daily attendance status: Present, Absent, Late, Half-Day, On Leave, Holiday.", "Must", "Phase 2"],
        ["FR-ATT-004", "Attendance", "System shall support timesheet entry and approval for non-shift employees.", "Should", "Phase 2"],
        ["FR-ATT-005", "Attendance", "System shall provide REST API endpoint for biometric device integration (clock event ingestion).", "Must", "Phase 2"],
        ["FR-ATT-006", "Attendance", "System shall support manual attendance correction by HR/Manager with mandatory reason.", "Must", "Phase 2"],
        ["FR-ATT-007", "Attendance", "System shall support overtime calculation per configured rules.", "Should", "Phase 2"],
        ["FR-ATT-008", "Attendance", "System shall integrate approved leave into attendance records automatically.", "Must", "Phase 2"],
        # PAYROLL
        ["FR-PAY-001", "Payroll", "System shall support salary structure definition: Basic, Housing, Transport, and other allowances.", "Must", "Phase 2"],
        ["FR-PAY-002", "Payroll", "System shall support employee salary assignment with effective date and history.", "Must", "Phase 2"],
        ["FR-PAY-003", "Payroll", "System shall calculate PAYE tax per Nigeria Tax Act bands (configurable annually).", "Must", "Phase 2"],
        ["FR-PAY-004", "Payroll", "System shall calculate Pension: 8% employee contribution + 10% employer contribution on qualifying emoluments.", "Must", "Phase 2"],
        ["FR-PAY-005", "Payroll", "System shall calculate NHF at 2.5% of basic salary (employee contribution).", "Must", "Phase 2"],
        ["FR-PAY-006", "Payroll", "System shall calculate NSITF employer contribution per configured rate on total emoluments.", "Must", "Phase 2"],
        ["FR-PAY-007", "Payroll", "System shall support additional deductions: loans, cooperative, union dues, custom deductions.", "Must", "Phase 2"],
        ["FR-PAY-008", "Payroll", "System shall support payroll run workflow: Draft → Calculated → Reviewed → Approved → Finalized.", "Must", "Phase 2"],
        ["FR-PAY-009", "Payroll", "System shall generate itemized payslip PDF per employee per period.", "Must", "Phase 2"],
        ["FR-PAY-010", "Payroll", "System shall generate bank payment file (CSV/TXT) for salary disbursement.", "Must", "Phase 2"],
        ["FR-PAY-011", "Payroll", "System shall lock finalized payroll periods against modification.", "Must", "Phase 2"],
        ["FR-PAY-012", "Payroll", "System shall factor approved unpaid leave and attendance into payroll calculations.", "Must", "Phase 2"],
        ["FR-PAY-013", "Payroll", "System shall store Pension PIN (PFA details) per employee.", "Must", "Phase 2"],
        ["FR-PAY-014", "Payroll", "System shall produce statutory remittance reports (PAYE, Pension, NHF summaries).", "Should", "Phase 2"],
        # REPORTS
        ["FR-RPT-001", "Reporting", "System shall provide headcount report by department, location, employment type, gender.", "Must", "Phase 2"],
        ["FR-RPT-002", "Reporting", "System shall provide attrition report with voluntary/involuntary breakdown.", "Must", "Phase 2"],
        ["FR-RPT-003", "Reporting", "System shall provide leave utilization and balance report.", "Must", "Phase 2"],
        ["FR-RPT-004", "Reporting", "System shall provide payroll cost summary by department and period.", "Must", "Phase 2"],
        ["FR-RPT-005", "Reporting", "All reports shall support date range filter and export to CSV/Excel.", "Must", "Phase 2"],
        ["FR-RPT-006", "Reporting", "System shall provide executive dashboard with KPI widgets.", "Should", "Phase 2"],
        # RECRUITMENT
        ["FR-REC-001", "Recruitment", "System shall support job posting CRUD with title, description, department, requirements, closing date.", "Must", "Phase 3"],
        ["FR-REC-002", "Recruitment", "System shall support applicant pipeline stages: Applied, Screening, Interview, Offer, Hired, Rejected.", "Must", "Phase 3"],
        ["FR-REC-003", "Recruitment", "System shall support resume/CV upload per candidate.", "Must", "Phase 3"],
        ["FR-REC-004", "Recruitment", "System shall generate offer letter from configurable template.", "Should", "Phase 3"],
        ["FR-REC-005", "Recruitment", "System shall convert hired candidate to employee record (pre-filled onboarding).", "Must", "Phase 3"],
        ["FR-REC-006", "Recruitment", "System shall support onboarding checklist with assignable tasks and due dates.", "Must", "Phase 3"],
        # PERFORMANCE
        ["FR-PERF-001", "Performance", "System shall support review cycle configuration (annual, mid-year, probation).", "Must", "Phase 3"],
        ["FR-PERF-002", "Performance", "System shall support goal/KPI setting per employee per cycle.", "Must", "Phase 3"],
        ["FR-PERF-003", "Performance", "System shall support self-assessment and manager assessment forms.", "Must", "Phase 3"],
        ["FR-PERF-004", "Performance", "System shall calculate weighted final rating from configured criteria.", "Must", "Phase 3"],
        ["FR-PERF-005", "Performance", "System shall support 360-degree feedback with anonymous peer input.", "Should", "Phase 3"],
        ["FR-PERF-006", "Performance", "System shall maintain performance history on employee profile.", "Must", "Phase 3"],
        # ADMIN
        ["FR-ADM-001", "Admin", "System shall support company profile configuration (name, logo, address, TIN, RC number).", "Must", "Phase 1"],
        ["FR-ADM-002", "Admin", "System shall support public holiday calendar management per year.", "Must", "Phase 1"],
        ["FR-ADM-003", "Admin", "System shall support configurable email templates for workflow notifications.", "Should", "Phase 1"],
        ["FR-ADM-004", "Admin", "System shall provide global audit log viewer with filter by user, entity, date range.", "Must", "Phase 1"],
    ]
    add_table(doc,
        ["FR ID", "Module", "Requirement", "Priority", "Phase"],
        fr_data)

    # 9. NFRs
    add_heading(doc, "9. Non-Functional Requirements", 1)
    add_table(doc,
        ["NFR ID", "Category", "Requirement", "Target/Standard"],
        [
            ["NFR-PERF-001", "Performance", "Page load time for dashboard (95th percentile)", "≤ 3 seconds on 10 Mbps connection"],
            ["NFR-PERF-002", "Performance", "API response time for standard CRUD (95th percentile)", "≤ 500 ms"],
            ["NFR-PERF-003", "Performance", "Payroll run for 2,000 employees", "≤ 10 minutes"],
            ["NFR-PERF-004", "Performance", "Concurrent authenticated users", "≥ 500 without degradation"],
            ["NFR-SCAL-001", "Scalability", "Support headcount growth without architecture change", "Up to 10,000 employees"],
            ["NFR-SCAL-002", "Scalability", "Database design supports horizontal read scaling (Azure SQL read replicas)", "Phase 2 readiness"],
            ["NFR-SEC-001", "Security", "All traffic encrypted via TLS 1.2+", "Mandatory"],
            ["NFR-SEC-002", "Security", "Passwords hashed with bcrypt or ASP.NET Identity PBKDF2", "Mandatory"],
            ["NFR-SEC-003", "Security", "JWT access token expiry", "15 minutes (configurable)"],
            ["NFR-SEC-004", "Security", "Sensitive PII fields encrypted at rest (bank account, NIN)", "AES-256"],
            ["NFR-SEC-005", "Security", "OWASP Top 10 mitigations implemented", "Mandatory"],
            ["NFR-AVAIL-001", "Availability", "System uptime during business hours (Mon–Sat 07:00–20:00 WAT)", "≥ 99.5%"],
            ["NFR-AVAIL-002", "Availability", "Planned maintenance window", "Sundays 00:00–04:00 WAT with 48hr notice"],
            ["NFR-AVAIL-003", "Disaster Recovery", "RPO / RTO", "RPO ≤ 1 hour; RTO ≤ 4 hours"],
            ["NFR-USAB-001", "Usability", "Responsive design for desktop, tablet, and mobile browsers", "Mandatory"],
            ["NFR-USAB-002", "Usability", "Microsoft Fluent UI design language", "Mandatory"],
            ["NFR-USAB-003", "Usability", "New employee completes first leave request without training", "≤ 5 minutes (usability test)"],
            ["NFR-ACC-001", "Accessibility", "WCAG 2.1 Level AA compliance for all employee-facing screens", "Mandatory"],
            ["NFR-LOC-001", "Localization", "Currency displayed as Nigerian Naira (₦ / NGN) with comma separators", "Mandatory"],
            ["NFR-LOC-002", "Localization", "Date format: DD/MM/YYYY; Timezone: WAT (UTC+1)", "Mandatory"],
            ["NFR-LOC-003", "Localization", "English (en-NG) as primary language; architecture supports future localization", "Phase 1"],
            ["NFR-AUD-001", "Auditability", "All create/update/delete on sensitive entities logged with actor, timestamp, old/new values", "Mandatory"],
            ["NFR-AUD-002", "Auditability", "Audit logs retained minimum 7 years", "Mandatory"],
            ["NFR-RET-001", "Data Retention", "Terminated employee data retained per company policy (default 7 years)", "Configurable"],
            ["NFR-RET-002", "Data Retention", "Soft-deleted records excluded from UI but retained in database", "Mandatory"],
            ["NFR-MAINT-001", "Maintainability", "Clean Architecture with vertical slice organization", "Mandatory"],
            ["NFR-MAINT-002", "Maintainability", "Code coverage for business logic (unit tests)", "≥ 80%"],
        ])

    # 10. Workflows
    add_heading(doc, "10. Workflow & Approval Logic", 1)

    add_heading(doc, "10.1 Leave Approval Workflow", 2)
    add_para(doc, "Flow Diagram (Text):", bold=True)
    add_para(doc,
        "[Employee] → Submit Leave Request → [System validates balance & dates]\n"
        "  ├─ Invalid → Return error to Employee\n"
        "  └─ Valid → Notify [Line Manager] → Pending Approval\n"
        "       ├─ Approve → (If HR approval required?) \n"
        "       │    ├─ Yes → Notify [HR Admin] → Approve/Reject\n"
        "       │    └─ No → Deduct balance → Notify Employee → Update Team Calendar\n"
        "       ├─ Reject → Notify Employee with reason\n"
        "       ├─ Return for Revision → Notify Employee\n"
        "       └─ No action within SLA (48h) → Escalate to [Next-Level Manager or HR]")
    add_table(doc,
        ["Step", "Actor", "Action", "System Behavior"],
        [
            ["1", "Employee", "Submit leave request", "Validate balance, dates, overlaps; set status = Pending; notify manager"],
            ["2", "Line Manager", "Review request", "View team calendar, balance, history; approve/reject/return"],
            ["3", "HR Admin (if configured)", "Final approval", "Required for maternity, unpaid, or exceeds threshold days"],
            ["4", "System", "Escalation", "After 48h pending: notify escalation recipient; log escalation event"],
            ["5", "System", "Completion", "Update balance, attendance preview, send email notification"],
        ])

    add_heading(doc, "10.2 Payroll Approval Workflow", 2)
    add_para(doc,
        "[Payroll Officer] → Initiate Payroll Run (Draft) → System calculates all employees\n"
        "  → [Payroll Officer] Reviews exceptions/variance report\n"
        "  → Submit for Approval → [HR Manager] reviews summary\n"
        "  → Approve → [CFO/Finance] final approval (configurable)\n"
        "  → Finalize → Generate payslips + bank file → Lock period\n"
        "  → Notify employees: payslips available")
    add_table(doc,
        ["Status", "Description", "Allowed Actions", "Next Status"],
        [
            ["Draft", "Payroll run initiated, calculations in progress", "Calculate, Cancel", "Calculated"],
            ["Calculated", "All employee pay computed; exceptions flagged", "Review, Recalculate, Submit", "Pending Approval"],
            ["Pending Approval", "Awaiting HR Manager / Finance approval", "Approve, Reject", "Approved / Calculated"],
            ["Approved", "Approved for disbursement", "Finalize", "Finalized"],
            ["Finalized", "Payslips generated, bank file exported, period locked", "None (reversal requires admin override)", "—"],
        ])

    add_heading(doc, "10.3 Profile Change Request Workflow", 2)
    add_para(doc,
        "[Employee] → Submit change (phone, address, etc.) → [HR Admin] Review\n"
        "  ├─ Approve → Update employee record → Notify employee\n"
        "  └─ Reject → Notify employee with reason")

    add_heading(doc, "10.4 Recruitment-to-Employee Workflow (Phase 3)", 2)
    add_para(doc,
        "[HR Admin] → Create Job Posting → [Candidate] Applies → Pipeline stages\n"
        "  → Offer extended → Candidate accepts → [HR Admin] Initiate Onboarding\n"
        "  → Create Employee record (pre-filled) → Onboarding checklist tasks assigned\n"
        "  → All tasks complete → Employee status = Active")

    # 11. Data Model
    add_heading(doc, "11. Data Model Overview", 1)
    add_para(doc, "11.1 Key Entities", bold=True)
    add_table(doc,
        ["Entity", "Description", "Key Attributes"],
        [
            ["Company", "Organization tenant", "Name, RCNumber, TIN, Address, LogoUrl, DefaultCurrency (NGN)"],
            ["Department", "Organizational unit", "Name, Code, ParentDepartmentId, HeadEmployeeId, IsActive"],
            ["Position", "Job title/role", "Title, Code, DepartmentId, GradeId, Description"],
            ["Grade", "Salary/level grade", "Name, Level, MinSalary, MaxSalary"],
            ["Employee", "Core person record", "EmployeeId, FirstName, LastName, Email, DOB, Gender, NIN, Status, DepartmentId, PositionId, ManagerId, JoinDate"],
            ["EmployeeBankAccount", "Payment details", "EmployeeId, BankName, AccountNumber (encrypted), AccountName"],
            ["EmployeeDocument", "Uploaded files", "EmployeeId, Category, FileName, StoragePath, ExpiryDate"],
            ["User", "Auth account", "Email, PasswordHash, EmployeeId, IsActive, LastLoginAt"],
            ["Role", "RBAC role", "Name, Description"],
            ["Permission", "Granular permission", "Module, Action, Resource"],
            ["RolePermission", "Role-permission mapping", "RoleId, PermissionId"],
            ["UserRole", "User-role assignment", "UserId, RoleId"],
            ["LeaveType", "Leave category", "Name, Code, IsPaid, RequiresAttachment, Color"],
            ["LeavePolicy", "Policy rules", "LeaveTypeId, AnnualEntitlement, AccrualFrequency, CarryForwardMax, ProbationMonths"],
            ["LeaveBalance", "Employee balance", "EmployeeId, LeaveTypeId, Year, Entitled, Used, Pending, CarriedForward"],
            ["LeaveRequest", "Leave application", "EmployeeId, LeaveTypeId, StartDate, EndDate, Days, Reason, Status, ApproverId"],
            ["LeaveApproval", "Approval step record", "LeaveRequestId, ApproverId, Action, Comments, ActionDate"],
            ["PublicHoliday", "Holiday calendar", "Date, Name, Year"],
            ["AttendanceRecord", "Daily attendance", "EmployeeId, Date, ClockIn, ClockOut, Status, Source, ShiftId"],
            ["Shift", "Work shift definition", "Name, StartTime, EndTime, GraceMinutes, BreakMinutes"],
            ["Timesheet", "Time entry", "EmployeeId, WeekStart, TotalHours, Status"],
            ["SalaryStructure", "Compensation template", "Name, Basic, Housing, Transport, OtherAllowances"],
            ["EmployeeSalary", "Employee pay assignment", "EmployeeId, SalaryStructureId, GrossSalary, EffectiveDate"],
            ["PayrollRun", "Payroll batch", "PeriodStart, PeriodEnd, Status, RunBy, ApprovedBy, FinalizedAt"],
            ["Payslip", "Employee pay record", "PayrollRunId, EmployeeId, GrossPay, PAYE, Pension, NHF, NSITF, NetPay, PdfPath"],
            ["PayrollDeduction", "Custom deduction", "EmployeeId, Type, Amount, IsRecurring"],
            ["JobPosting", "Open position", "Title, DepartmentId, Description, Status, ClosingDate"],
            ["Candidate", "Applicant", "JobPostingId, Name, Email, Phone, Stage, ResumePath"],
            ["OnboardingTask", "Onboarding checklist item", "EmployeeId, TaskName, AssignedTo, DueDate, IsCompleted"],
            ["ReviewCycle", "Performance period", "Name, StartDate, EndDate, Status"],
            ["EmployeeGoal", "KPI/goal", "EmployeeId, ReviewCycleId, Title, Weight, TargetValue, ActualValue"],
            ["PerformanceReview", "Appraisal record", "EmployeeId, ReviewCycleId, SelfRating, ManagerRating, FinalRating, Status"],
            ["AuditLog", "Change audit trail", "EntityName, EntityId, Action, OldValues, NewValues, UserId, Timestamp"],
            ["Notification", "In-app notification", "UserId, Title, Message, IsRead, CreatedAt"],
        ])

    add_para(doc, "11.2 Entity Relationships (ER Description)", bold=True)
    er_desc = [
        "Company has many Departments; Department has optional parent Department (self-referencing hierarchy).",
        "Department has many Positions; Position belongs to one Department and optional Grade.",
        "Employee belongs to one Department, one Position, and one Manager (self-referencing Employee).",
        "Employee has one User account (1:1) for authentication.",
        "Employee has many EmployeeDocuments, LeaveBalances, LeaveRequests, AttendanceRecords, Payslips.",
        "LeaveType has one LeavePolicy; LeaveBalance is unique per Employee + LeaveType + Year.",
        "LeaveRequest has many LeaveApprovals (one per approval step).",
        "SalaryStructure is assigned to Employees via EmployeeSalary (with effective date history).",
        "PayrollRun has many Payslips; each Payslip belongs to one Employee.",
        "JobPosting has many Candidates; hired Candidate converts to Employee.",
        "ReviewCycle has many EmployeeGoals and PerformanceReviews.",
        "All entities include audit columns (CreatedBy, CreatedAt, ModifiedBy, ModifiedAt) and IsDeleted for soft delete.",
    ]
    for e in er_desc:
        doc.add_paragraph(e, style="List Bullet")

    # 12. Integrations
    add_heading(doc, "12. Integration Requirements", 1)
    add_table(doc,
        ["Integration", "Purpose", "Protocol/Method", "Phase", "Notes"],
        [
            ["SMTP Email (SendGrid / Azure Communication Services)", "Workflow notifications, password reset, payslip alerts", "SMTP / REST API", "Phase 1", "HTML email templates; configurable sender address"],
            ["Biometric Attendance Devices", "Ingest clock-in/out events", "REST API (webhook/poll)", "Phase 2", "Vendor-agnostic endpoint; support ZKTeco and similar via middleware"],
            ["Bank Payment Gateway / File Export", "Salary disbursement", "CSV/TXT file (NIBSS/NEFT format)", "Phase 2", "Configurable per bank template; no direct API in initial release"],
            ["Azure Blob Storage", "Document and payslip PDF storage", "Azure SDK", "Phase 1", "SAS token access; encrypted at rest"],
            ["Azure AD / OIDC (Future)", "Enterprise SSO", "OpenID Connect", "Post Phase 1", "Architecture reserves ExternalAuthProvider table"],
            ["FIRS / State IRS (Future)", "Tax filing automation", "TBD", "Later", "Manual export initially; API when available"],
            ["PFA Pension Platforms (Future)", "Pension remittance", "TBD", "Later", "Export pension schedule CSV initially"],
            ["Microsoft Excel/CSV Export", "Report data export", "File download", "Phase 2", "All reports exportable"],
            ["Hangfire Scheduler", "Background jobs: accruals, escalations, payroll", "In-process", "Phase 1", "Recurring jobs for leave accrual and approval escalation"],
        ])

    # 13. Security & Compliance
    add_heading(doc, "13. Security & Compliance", 1)
    add_para(doc, "13.1 Nigeria Data Protection Regulation (NDPR) Compliance", bold=True)
    ndpr_items = [
        "Lawful basis for processing: employment contract and legitimate business interest.",
        "Data minimization: collect only HR-necessary personal data.",
        "Consent captured where required (e.g., optional fields, 360 feedback participation).",
        "Right to access: employees can view their own data via ESS.",
        "Right to rectification: profile change request workflow.",
        "Data breach notification procedure documented (notify NITDA within 72 hours).",
        "Data Protection Officer (DPO) contact published in system footer.",
        "Cross-border transfer: all data hosted in Azure region with NDPR-compliant DPA.",
        "Privacy policy and data processing agreement templates provided.",
    ]
    for item in ndpr_items:
        doc.add_paragraph(item, style="List Bullet")

    add_para(doc, "13.2 Encryption & Data Protection", bold=True)
    add_table(doc,
        ["Data Element", "Protection Method"],
        [
            ["Passwords", "Hashed (PBKDF2/bcrypt); never stored in plain text"],
            ["JWT Tokens", "Signed (HMAC-SHA256); short-lived access tokens"],
            ["Bank account numbers", "AES-256 encryption at rest; masked in UI (show last 4 digits)"],
            ["NIN", "AES-256 encryption at rest; restricted to HR Admin role"],
            ["Documents (ID, contracts)", "Azure Blob Storage with encryption at rest; RBAC-controlled SAS URLs"],
            ["Data in transit", "TLS 1.2+ on all endpoints"],
            ["SQL Server", "Transparent Data Encryption (TDE) enabled on Azure SQL"],
        ])

    add_para(doc, "13.3 RBAC Matrix (Summary)", bold=True)
    add_para(doc, "Full matrix in Appendix A. Key principles:")
    rbac_principles = [
        "Least privilege: users receive minimum permissions for their role.",
        "Separation of duties: payroll finalize requires different user than payroll calculate.",
        "Manager scope: automatically limited to direct/indirect reports.",
        "Audit: all permission changes logged.",
    ]
    for r in rbac_principles:
        doc.add_paragraph(r, style="List Bullet")

    # 14. Reporting
    add_heading(doc, "14. Reporting & Analytics Requirements", 1)
    add_table(doc,
        ["Report ID", "Report Name", "Audience", "Phase", "Key Fields"],
        [
            ["RPT-001", "Headcount Summary", "HR, Executive", "Phase 2", "Department, location, gender, employment type, count"],
            ["RPT-002", "Headcount Trend", "Executive", "Phase 2", "Month-over-month headcount, new hires, exits"],
            ["RPT-003", "Attrition Report", "HR, Executive", "Phase 2", "Voluntary/involuntary exits, attrition rate, department breakdown"],
            ["RPT-004", "Leave Balance Report", "HR", "Phase 1", "Employee, leave type, entitled, used, remaining"],
            ["RPT-005", "Leave Utilization", "HR, Managers", "Phase 2", "Department, leave type, days taken, % utilization"],
            ["RPT-006", "Leave History", "HR", "Phase 1", "Employee, dates, type, status, approver"],
            ["RPT-007", "Attendance Summary", "HR, Managers", "Phase 2", "Employee, days present, absent, late, on leave"],
            ["RPT-008", "Payroll Register", "Payroll, Finance", "Phase 2", "Employee, gross, each deduction, net pay"],
            ["RPT-009", "Payroll Cost by Department", "Finance, Executive", "Phase 2", "Department, total gross, total net, employer pension"],
            ["RPT-010", "PAYE Remittance Schedule", "Payroll, Finance", "Phase 2", "Employee, taxable income, PAYE amount"],
            ["RPT-011", "Pension Remittance Schedule", "Payroll, Finance", "Phase 2", "Employee, PFA, PIN, employee/employer contributions"],
            ["RPT-012", "NHF Schedule", "Payroll, Finance", "Phase 2", "Employee, basic salary, NHF amount"],
            ["RPT-013", "Bank Payment File", "Payroll, Finance", "Phase 2", "Account number, bank code, net pay, narration"],
            ["RPT-014", "Employee Master List", "HR", "Phase 1", "All core employee fields, exportable CSV"],
            ["RPT-015", "Birthday/Anniversary", "HR", "Phase 1", "Employee, DOB, join date, upcoming dates"],
            ["RPT-016", "Recruitment Pipeline", "HR", "Phase 3", "Job, stage, candidate count, time-in-stage"],
            ["RPT-017", "Performance Rating Distribution", "HR, Executive", "Phase 3", "Department, rating scale, count, average"],
            ["RPT-018", "Onboarding Status", "HR", "Phase 3", "Employee, tasks completed, overdue tasks"],
        ])

    add_para(doc, "Dashboard Widgets", bold=True)
    add_table(doc,
        ["Dashboard", "Widgets"],
        [
            ["ESS Dashboard", "Leave balance, pending requests, notifications, quick actions"],
            ["MSS Dashboard", "Pending approvals, team on leave today, team headcount, upcoming birthdays"],
            ["HR Admin Dashboard", "Total headcount, new joiners, exits, pending approvals, leave trends chart"],
            ["Executive Dashboard", "Headcount trend, attrition rate, payroll cost, department breakdown, gender diversity"],
            ["Payroll Dashboard", "Current period status, exceptions, total payroll cost, comparison to prior month"],
        ])

    # 15. Technical Architecture
    add_heading(doc, "15. Technical Architecture Overview", 1)
    add_para(doc, "15.1 Architecture Layers", bold=True)
    add_table(doc,
        ["Layer", "Technology", "Responsibility"],
        [
            ["Presentation", "Angular 17+ (standalone components, lazy-loaded feature modules)", "UI, routing, form validation, Fluent UI components, API consumption"],
            ["API Gateway / Host", "ASP.NET Core 8 Web API", "HTTP endpoints, JWT auth middleware, CORS, Swagger/OpenAPI"],
            ["Application", "MediatR (CQRS), FluentValidation, AutoMapper", "Commands, queries, handlers, DTOs, validation, mapping"],
            ["Domain", "C# class library (entities, enums, domain events, interfaces)", "Business rules, domain models, repository interfaces"],
            ["Infrastructure", "EF Core, SQL Server, Hangfire, Serilog, Azure Blob", "Data access, migrations, background jobs, logging, file storage"],
        ])

    add_para(doc, "15.2 Key Patterns & Rationale", bold=True)
    patterns = [
        "Clean Architecture: dependency inversion ensures domain logic is independent of infrastructure; supports testability and maintainability.",
        "Vertical Slice Architecture: features organized by use case (e.g., Leave/ApplyLeave, Leave/ApproveLeave) rather than technical layer; reduces cross-cutting coupling.",
        "CQRS via MediatR: separates read and write operations; each handler is a single-responsibility unit.",
        "Repository + Unit of Work: abstracts data access; EF Core DbContext implements Unit of Work.",
        "Soft Deletes: global query filters on IsDeleted; preserves data integrity and audit trail.",
        "Audit Interceptor: EF Core SaveChanges interceptor auto-populates audit columns and writes to AuditLog.",
        "Background Jobs (Hangfire): scheduled leave accrual, approval escalation, payroll processing, email dispatch.",
        "Structured Logging (Serilog): JSON logs to Azure Application Insights for monitoring and alerting.",
    ]
    for p in patterns:
        doc.add_paragraph(p, style="List Bullet")

    add_para(doc, "15.3 Frontend Architecture", bold=True)
    fe_items = [
        "Angular standalone components with lazy-loaded feature modules per domain (employee, leave, payroll, etc.).",
        "Fluent UI Blazor-inspired design via @fluentui/web-components or custom Fluent theme on Angular Material.",
        "NgRx or Angular Signals for state management in complex modules (payroll, leave).",
        "Route guards for RBAC enforcement; HTTP interceptor for JWT attachment and refresh.",
        "Reactive forms with client-side validation matching FluentValidation rules.",
    ]
    for f in fe_items:
        doc.add_paragraph(f, style="List Bullet")

    add_para(doc, "15.4 Azure Hosting Topology", bold=True)
    add_table(doc,
        ["Azure Service", "Purpose"],
        [
            ["Azure App Service (API)", "Host ASP.NET Core Web API"],
            ["Azure App Service (Web)", "Host Angular SPA (or Azure Static Web Apps)"],
            ["Azure SQL Database", "Primary relational data store (Business Critical tier for production)"],
            ["Azure Blob Storage", "Document and payslip PDF storage"],
            ["Azure Application Insights", "APM, logging, alerting"],
            ["Azure Key Vault", "Secrets management (connection strings, JWT keys, encryption keys)"],
            ["Azure Cache for Redis (optional)", "Session/token caching at scale"],
        ])

    # 16. Assumptions
    add_heading(doc, "16. Assumptions, Constraints & Dependencies", 1)
    add_table(doc,
        ["ID", "Type", "Description"],
        [
            ["A-01", "Assumption", "Client will provide Nigerian public holiday calendar annually."],
            ["A-02", "Assumption", "Client will provide current PAYE tax bands and update when Finance Act changes."],
            ["A-03", "Assumption", "Client has existing employee data available for migration in CSV format."],
            ["A-04", "Assumption", "Employees have corporate email addresses for authentication."],
            ["A-05", "Assumption", "Client IT team will provision Azure resources per architecture spec."],
            ["A-06", "Assumption", "Biometric device vendor will provide API/SDK for clock event integration."],
            ["A-07", "Constraint", "Nigeria-only payroll compliance in scope; no multi-currency payroll."],
            ["A-08", "Constraint", "Web-responsive only; no native mobile apps in Phases 1–3."],
            ["A-09", "Constraint", "Single-tenant deployment per client instance (not multi-tenant SaaS in v1)."],
            ["A-10", "Constraint", "English (en-NG) only UI in initial releases."],
            ["A-11", "Dependency", "Azure subscription with appropriate service quotas."],
            ["A-12", "Dependency", "SMTP relay or Azure Communication Services for email delivery."],
            ["A-13", "Dependency", "SSL certificate for custom domain."],
            ["A-14", "Dependency", "Client HR team availability for UAT and policy decisions during each phase."],
            ["A-15", "Dependency", "Client Finance team for payroll validation and bank file format confirmation."],
        ])

    # 17. Release Plan
    add_heading(doc, "17. Release Plan / Phased Roadmap", 1)
    add_table(doc,
        ["Phase", "Milestone", "Key Deliverables", "Exit Criteria"],
        [
            ["Phase 1", "M1: Foundation", "Project scaffold, DB schema, auth, RBAC, CI/CD pipeline", "Users can authenticate; roles assigned; audit logging operational"],
            ["Phase 1", "M2: Employee Core", "Employee CRUD, departments, positions, org chart, documents", "HR can onboard employees; directory searchable; profiles viewable"],
            ["Phase 1", "M3: Leave & ESS/MSS", "Leave types/policies, apply/approve, balances, dashboards, notifications", "End-to-end leave workflow live; 95% of employees onboarded"],
            ["Phase 1", "M4: UAT & Go-Live", "UAT, bug fixes, data migration, training, production deployment", "Phase 1 sign-off; go-live approval from HR & IT"],
            ["Phase 2", "M5: Attendance", "Clock-in/out, shifts, timesheets, device API, attendance reports", "Attendance captured for 90% of employees"],
            ["Phase 2", "M6: Payroll", "Salary structures, PAYE/Pension/NHF/NSITF, payroll run, payslips, bank file", "First live payroll run completed with < 2% adjustments"],
            ["Phase 2", "M7: Analytics", "Dashboards, headcount, attrition, leave/payroll reports", "Executive dashboard live; reports validated by Finance"],
            ["Phase 2", "M8: UAT & Go-Live", "UAT, parallel payroll run, go-live", "Phase 2 sign-off; parallel payroll reconciled"],
            ["Phase 3", "M9: Recruitment", "Job postings, ATS pipeline, offer letters, onboarding checklists", "First hire processed end-to-end in system"],
            ["Phase 3", "M10: Performance", "Review cycles, goals, appraisals, 360 feedback", "First review cycle completed in system"],
            ["Phase 3", "M11: UAT & Go-Live", "UAT, training, go-live", "Phase 3 sign-off"],
            ["Later", "M12+", "Training & Development, Benefits & Compliance, SSO, mobile apps", "Per separate PRD"],
        ])

    # 18. Risks
    add_heading(doc, "18. Risks & Mitigations", 1)
    add_table(doc,
        ["Risk ID", "Risk", "Likelihood", "Impact", "Mitigation"],
        [
            ["RK-01", "Incorrect PAYE/tax calculation due to regulatory changes", "Medium", "High", "Configurable tax bands; annual review process with Finance; automated unit tests against known scenarios"],
            ["RK-02", "Low employee adoption of self-service", "Medium", "High", "Change management program; simple UX; training sessions; manager champions"],
            ["RK-03", "Data migration errors from legacy spreadsheets", "High", "High", "Migration validation scripts; parallel run period; HR sign-off on migrated data"],
            ["RK-04", "Scope creep across phases", "High", "Medium", "Strict MoSCoW prioritization; change request process; phase gate reviews"],
            ["RK-05", "Biometric device integration incompatibility", "Medium", "Medium", "Vendor-agnostic API; early POC with client's device; manual entry fallback"],
            ["RK-06", "Performance degradation at scale", "Low", "High", "Load testing at 2x expected headcount; Azure SQL tuning; pagination on all lists"],
            ["RK-07", "NDPR non-compliance", "Low", "High", "DPO review; encryption; access controls; privacy impact assessment"],
            ["RK-08", "Payroll run failure mid-processing", "Low", "High", "Transactional payroll processing; idempotent runs; rollback capability; Hangfire retry policies"],
            ["RK-09", "Key person dependency on client HR team for UAT", "Medium", "Medium", "Early engagement; documented test cases; dedicated UAT window with backups"],
            ["RK-10", "Azure service outage", "Low", "High", "Azure SLA reliance; DR plan with geo-redundant backups; communication plan"],
            ["RK-11", "Security breach / unauthorized access", "Low", "Critical", "Penetration testing; OWASP compliance; MFA readiness; audit logging; incident response plan"],
            ["RK-12", "Bank file format rejection", "Medium", "High", "Early confirmation of bank template with Finance; test file submission before go-live"],
        ])

    # 19. Glossary
    add_heading(doc, "19. Glossary of Terms", 1)
    add_table(doc,
        ["Term", "Definition"],
        [
            ["CRAS", "Consolidated Relief Allowance — deducted from taxable income before PAYE calculation per Nigeria Tax Act."],
            ["ESS", "Employee Self-Service — portal for employees to manage their own HR tasks."],
            ["FIRS", "Federal Inland Revenue Service — Nigeria's federal tax authority."],
            ["MSS", "Manager Self-Service — portal for line managers to approve requests and view team data."],
            ["NDPR", "Nigeria Data Protection Regulation — data privacy law governing personal data processing."],
            ["NHF", "National Housing Fund — 2.5% employee contribution on basic salary for housing finance."],
            ["NIN", "National Identification Number — Nigeria's national ID number."],
            ["NIBSS", "Nigeria Inter-Bank Settlement System — facilitates electronic fund transfers between banks."],
            ["NSITF", "Nigeria Social Insurance Trust Fund — employer contribution for employee compensation insurance."],
            ["PAYE", "Pay As You Earn — progressive income tax deducted at source from employee salaries."],
            ["PFA", "Pension Fund Administrator — licensed entity managing employee pension contributions."],
            ["PENCOM", "National Pension Commission — regulatory body for pension in Nigeria."],
            ["PFI", "Pension Fund Custodian — holds pension assets on behalf of PFAs."],
            ["RBAC", "Role-Based Access Control — authorization model based on user roles."],
            ["RC Number", "Registration Certificate Number — company registration identifier from CAC."],
            ["SLA", "Service Level Agreement — agreed response/resolution time for workflow actions."],
            ["SSO", "Single Sign-On — authentication via external identity provider (e.g., Azure AD)."],
            ["TIN", "Tax Identification Number — issued by FIRS for tax purposes."],
            ["WAT", "West Africa Time — UTC+1, Nigeria's standard timezone."],
        ])

    # 20. Appendix
    add_heading(doc, "20. Appendix", 1)
    add_heading(doc, "Appendix A: RBAC Permission Matrix", 2)
    add_table(doc,
        ["Permission / Module", "Employee", "Line Manager", "HR Admin", "HR Manager", "Payroll Officer", "System Admin", "Executive"],
        [
            ["View own profile", "✓", "✓", "✓", "✓", "✓", "—", "—"],
            ["Edit own profile (limited)", "✓", "✓", "✓", "✓", "✓", "—", "—"],
            ["View team profiles", "—", "✓", "✓", "✓", "—", "—", "—"],
            ["View all employee profiles", "—", "—", "✓", "✓", "✓", "—", "—"],
            ["Create/edit employee records", "—", "—", "✓", "✓", "—", "—", "—"],
            ["Deactivate employee", "—", "—", "✓", "✓", "—", "—", "—"],
            ["Manage departments/positions", "—", "—", "✓", "✓", "—", "—", "—"],
            ["Upload/view own documents", "✓", "✓", "✓", "✓", "✓", "—", "—"],
            ["Upload/view all documents", "—", "—", "✓", "✓", "—", "—", "—"],
            ["Apply for leave", "✓", "✓", "✓", "✓", "✓", "—", "—"],
            ["Approve team leave", "—", "✓", "✓", "✓", "—", "—", "—"],
            ["Approve all leave / override", "—", "—", "✓", "✓", "—", "—", "—"],
            ["Configure leave types/policies", "—", "—", "✓", "✓", "—", "—", "—"],
            ["Adjust leave balances", "—", "—", "✓", "✓", "—", "—", "—"],
            ["Clock in/out", "✓", "✓", "✓", "✓", "✓", "—", "—"],
            ["View team attendance", "—", "✓", "✓", "✓", "—", "—", "—"],
            ["Manage attendance/shifts", "—", "—", "✓", "✓", "—", "—", "—"],
            ["View own payslips", "—", "—", "—", "—", "✓", "—", "—"],
            ["Manage salary structures", "—", "—", "—", "—", "✓", "—", "—"],
            ["Run/approve payroll", "—", "—", "—", "✓", "✓", "—", "—"],
            ["Export bank payment file", "—", "—", "—", "—", "✓", "—", "—"],
            ["Manage recruitment", "—", "—", "✓", "✓", "—", "—", "—"],
            ["Manage performance reviews", "—", "✓", "✓", "✓", "—", "—", "—"],
            ["View HR reports", "—", "—", "✓", "✓", "✓", "—", "—"],
            ["View executive dashboards", "—", "—", "—", "✓", "—", "—", "✓"],
            ["Manage roles/permissions", "—", "—", "—", "—", "—", "✓", "—"],
            ["View audit logs", "—", "—", "—", "—", "—", "✓", "—"],
            ["System configuration", "—", "—", "—", "—", "—", "✓", "—"],
        ])

    add_heading(doc, "Appendix B: Nigeria PAYE Tax Bands (2024 Finance Act Reference)", 2)
    add_para(doc, "Note: Tax bands must be configurable in the system to accommodate annual Finance Act changes. Below reflects the 2024 reference structure.")
    add_table(doc,
        ["Annual Taxable Income (₦)", "Tax Rate"],
        [
            ["First ₦800,000", "0%"],
            ["Next ₦2,200,000", "15%"],
            ["Next ₦9,000,000", "18%"],
            ["Next ₦13,000,000", "21%"],
            ["Next ₦25,000,000", "23%"],
            ["Above ₦50,000,000", "25%"],
        ])
    add_para(doc, "Consolidated Relief Allowance (CRA): Higher of ₦200,000 or 1% of gross income, plus 20% of gross income. Applied before band calculation.")

    add_heading(doc, "Appendix C: Nigeria Statutory Deductions Summary", 2)
    add_table(doc,
        ["Deduction", "Rate", "Paid By", "Basis", "Regulatory Body"],
        [
            ["PAYE", "Progressive (see Appendix B)", "Employee", "Taxable income after CRA", "FIRS / State IRS"],
            ["Pension", "8%", "Employee", "Monthly emoluments (basic + housing + transport)", "PENCOM"],
            ["Pension", "10%", "Employer", "Monthly emoluments", "PENCOM"],
            ["NHF", "2.5%", "Employee", "Basic salary", "Federal Mortgage Bank"],
            ["NSITF", "1% (configurable)", "Employer", "Total monthly emoluments", "NSITF"],
        ])

    add_heading(doc, "Appendix D: Sample Report Output Fields", 2)
    add_table(doc,
        ["Report", "Sample Columns"],
        [
            ["Payroll Register", "Employee ID, Name, Department, Basic, Housing, Transport, Gross, PAYE, Pension (EE), Pension (ER), NHF, NSITF, Other Deductions, Net Pay"],
            ["Headcount", "Department, Location, Active Count, On Leave, New Hires (MTD), Exits (MTD)"],
            ["Attrition", "Period, Department, Opening HC, Exits, Attrition Rate, Voluntary, Involuntary"],
            ["Leave Utilization", "Employee, Department, Leave Type, Entitled, Used, Remaining, Utilization %"],
            ["Bank Payment File", "S/N, Employee Name, Bank Code, Account Number, Amount (₦), Narration"],
        ])

    add_heading(doc, "Appendix E: API Endpoint Conventions", 2)
    add_table(doc,
        ["Convention", "Standard"],
        [
            ["Base URL", "https://api.employee360.{client-domain}/api/v1"],
            ["Authentication", "Bearer JWT in Authorization header"],
            ["Versioning", "URL path versioning (/api/v1/...)"],
            ["Pagination", "Query params: page, pageSize (default 20, max 100)"],
            ["Sorting", "Query param: sortBy, sortDirection (asc/desc)"],
            ["Error format", "RFC 7807 Problem Details (application/problem+json)"],
            ["Date format", "ISO 8601 (YYYY-MM-DDTHH:mm:ss+01:00)"],
            ["Currency", "Decimal (18,2) stored; displayed as ₦ with 2 decimal places"],
        ])

    add_heading(doc, "Appendix F: API Module Endpoints (Reference)", 2)
    add_table(doc,
        ["Module", "Method", "Endpoint", "Description", "Phase"],
        [
            ["Auth", "POST", "/api/v1/auth/login", "Authenticate user; return JWT", "Phase 1"],
            ["Auth", "POST", "/api/v1/auth/refresh", "Refresh access token", "Phase 1"],
            ["Auth", "POST", "/api/v1/auth/forgot-password", "Initiate password reset", "Phase 1"],
            ["Employees", "GET", "/api/v1/employees", "List employees (paginated, filterable)", "Phase 1"],
            ["Employees", "POST", "/api/v1/employees", "Create employee", "Phase 1"],
            ["Employees", "GET", "/api/v1/employees/{id}", "Get employee by ID", "Phase 1"],
            ["Employees", "PUT", "/api/v1/employees/{id}", "Update employee", "Phase 1"],
            ["Employees", "GET", "/api/v1/employees/org-chart", "Get org chart tree", "Phase 1"],
            ["Departments", "CRUD", "/api/v1/departments", "Manage departments", "Phase 1"],
            ["Positions", "CRUD", "/api/v1/positions", "Manage positions", "Phase 1"],
            ["Documents", "POST", "/api/v1/employees/{id}/documents", "Upload document", "Phase 1"],
            ["Leave", "POST", "/api/v1/leave/requests", "Submit leave request", "Phase 1"],
            ["Leave", "PUT", "/api/v1/leave/requests/{id}/approve", "Approve leave", "Phase 1"],
            ["Leave", "GET", "/api/v1/leave/balances/{employeeId}", "Get leave balances", "Phase 1"],
            ["Leave", "GET", "/api/v1/leave/calendar", "Team leave calendar", "Phase 1"],
            ["Attendance", "POST", "/api/v1/attendance/clock-in", "Clock in", "Phase 2"],
            ["Attendance", "POST", "/api/v1/attendance/clock-out", "Clock out", "Phase 2"],
            ["Attendance", "POST", "/api/v1/attendance/device-events", "Biometric device ingestion", "Phase 2"],
            ["Payroll", "POST", "/api/v1/payroll/runs", "Initiate payroll run", "Phase 2"],
            ["Payroll", "GET", "/api/v1/payroll/runs/{id}/payslips", "List payslips for run", "Phase 2"],
            ["Payroll", "GET", "/api/v1/payroll/payslips/{id}/download", "Download payslip PDF", "Phase 2"],
            ["Payroll", "GET", "/api/v1/payroll/runs/{id}/bank-file", "Export bank payment file", "Phase 2"],
            ["Reports", "GET", "/api/v1/reports/{reportId}", "Generate report", "Phase 2"],
            ["Recruitment", "CRUD", "/api/v1/jobs", "Manage job postings", "Phase 3"],
            ["Recruitment", "CRUD", "/api/v1/candidates", "Manage candidates", "Phase 3"],
            ["Performance", "CRUD", "/api/v1/reviews", "Manage performance reviews", "Phase 3"],
            ["Admin", "GET", "/api/v1/audit-logs", "Query audit logs", "Phase 1"],
            ["Admin", "CRUD", "/api/v1/roles", "Manage roles and permissions", "Phase 1"],
        ])

    add_heading(doc, "Appendix G: UI Screen Inventory", 2)
    add_table(doc,
        ["Screen ID", "Screen Name", "Module", "Roles", "Phase"],
        [
            ["SCR-001", "Login", "Auth", "All", "Phase 1"],
            ["SCR-002", "Forgot Password", "Auth", "All", "Phase 1"],
            ["SCR-003", "ESS Dashboard", "Self-Service", "Employee", "Phase 1"],
            ["SCR-004", "MSS Dashboard", "Self-Service", "Line Manager", "Phase 1"],
            ["SCR-005", "Employee Directory", "Employee", "HR Admin, Manager", "Phase 1"],
            ["SCR-006", "Employee Profile", "Employee", "All (scoped)", "Phase 1"],
            ["SCR-007", "Create/Edit Employee", "Employee", "HR Admin", "Phase 1"],
            ["SCR-008", "Org Chart", "Employee", "All", "Phase 1"],
            ["SCR-009", "Department Management", "Employee", "HR Admin", "Phase 1"],
            ["SCR-010", "Position Management", "Employee", "HR Admin", "Phase 1"],
            ["SCR-011", "Apply for Leave", "Leave", "Employee", "Phase 1"],
            ["SCR-012", "My Leave Requests", "Leave", "Employee", "Phase 1"],
            ["SCR-013", "Leave Approval Queue", "Leave", "Manager, HR", "Phase 1"],
            ["SCR-014", "Team Leave Calendar", "Leave", "Manager", "Phase 1"],
            ["SCR-015", "Leave Policy Configuration", "Leave", "HR Admin", "Phase 1"],
            ["SCR-016", "Leave Balance Management", "Leave", "HR Admin", "Phase 1"],
            ["SCR-017", "Public Holiday Calendar", "Admin", "HR Admin", "Phase 1"],
            ["SCR-018", "Company Settings", "Admin", "HR Admin, System Admin", "Phase 1"],
            ["SCR-019", "Role & Permission Management", "Admin", "System Admin", "Phase 1"],
            ["SCR-020", "Audit Log Viewer", "Admin", "System Admin, HR Manager", "Phase 1"],
            ["SCR-021", "Clock In/Out", "Attendance", "Employee", "Phase 2"],
            ["SCR-022", "Attendance Log", "Attendance", "Employee, Manager, HR", "Phase 2"],
            ["SCR-023", "Shift Management", "Attendance", "HR Admin", "Phase 2"],
            ["SCR-024", "Timesheet Entry", "Attendance", "Employee", "Phase 2"],
            ["SCR-025", "Salary Structure Management", "Payroll", "Payroll Officer", "Phase 2"],
            ["SCR-026", "Employee Salary Assignment", "Payroll", "Payroll Officer", "Phase 2"],
            ["SCR-027", "Payroll Run", "Payroll", "Payroll Officer", "Phase 2"],
            ["SCR-028", "Payroll Approval", "Payroll", "HR Manager, Finance", "Phase 2"],
            ["SCR-029", "My Payslips", "Payroll", "Employee", "Phase 2"],
            ["SCR-030", "Reports Dashboard", "Reports", "HR, Executive", "Phase 2"],
            ["SCR-031", "Job Postings", "Recruitment", "HR Admin", "Phase 3"],
            ["SCR-032", "Candidate Pipeline", "Recruitment", "HR Admin", "Phase 3"],
            ["SCR-033", "Onboarding Checklist", "Recruitment", "HR Admin, Employee", "Phase 3"],
            ["SCR-034", "Review Cycle Management", "Performance", "HR Admin", "Phase 3"],
            ["SCR-035", "My Goals & Appraisal", "Performance", "Employee", "Phase 3"],
            ["SCR-036", "Team Appraisals", "Performance", "Line Manager", "Phase 3"],
        ])

    add_heading(doc, "Appendix H: Database Indexing Strategy", 2)
    add_table(doc,
        ["Table", "Index", "Rationale"],
        [
            ["Employee", "IX_Employee_DepartmentId_Status", "Directory filtering by department and status"],
            ["Employee", "IX_Employee_ManagerId", "Manager team queries and org chart"],
            ["Employee", "IX_Employee_Email (unique)", "Login lookup"],
            ["LeaveRequest", "IX_LeaveRequest_EmployeeId_Status", "Employee leave history and pending queries"],
            ["LeaveRequest", "IX_LeaveRequest_ApproverId_Status", "Manager approval queue"],
            ["AttendanceRecord", "IX_Attendance_EmployeeId_Date (unique)", "Daily attendance lookup"],
            ["Payslip", "IX_Payslip_EmployeeId_Period", "Payslip history retrieval"],
            ["AuditLog", "IX_AuditLog_EntityName_EntityId", "Entity change history"],
            ["AuditLog", "IX_AuditLog_Timestamp", "Date-range audit queries"],
        ])

    add_heading(doc, "Appendix I: Email Notification Templates", 2)
    add_table(doc,
        ["Template ID", "Trigger", "Recipient", "Subject Line"],
        [
            ["EML-001", "Leave submitted", "Line Manager", "Leave Request from {EmployeeName} — Action Required"],
            ["EML-002", "Leave approved", "Employee", "Your Leave Request has been Approved"],
            ["EML-003", "Leave rejected", "Employee", "Your Leave Request has been Declined"],
            ["EML-004", "Leave escalated", "Escalation Manager", "Escalated: Pending Leave Approval for {EmployeeName}"],
            ["EML-005", "Welcome / account created", "New Employee", "Welcome to Employee360 — Set Up Your Account"],
            ["EML-006", "Password reset", "User", "Employee360 Password Reset Request"],
            ["EML-007", "Payslip available", "Employee", "Your Payslip for {Period} is Ready"],
            ["EML-008", "Payroll pending approval", "HR Manager", "Payroll Run {Period} — Approval Required"],
            ["EML-009", "Profile change approved", "Employee", "Your Profile Update has been Approved"],
            ["EML-010", "Onboarding task assigned", "Task Assignee", "Onboarding Task Assigned: {TaskName}"],
        ])

    doc.save(OUTPUT)
    print(f"Generated: {OUTPUT}")


if __name__ == "__main__":
    build_document()
