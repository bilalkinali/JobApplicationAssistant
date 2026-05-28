import { Field, Select } from "../../components/FormControls";
import { StatusBadge } from "../../components/StatusBadge";
import type { ApplicationSession } from "../shared/types";

type ApplicationListPanelProps = {
  applications: ApplicationSession[];
  filteredApplications: ApplicationSession[];
  selectedApplicationId: string | null;
  approvedProfileFactCount: number;
  includeArchivedApplications: boolean;
  applicationSearch: string;
  applicationStatusFilter: string;
  applicationReadinessFilter: string;
  applicationStatuses: string[];
  auditReadinessOptions: string[];
  onSearchChange: (value: string) => void;
  onStatusFilterChange: (value: string) => void;
  onReadinessFilterChange: (value: string) => void;
  onIncludeArchivedChange: (value: boolean) => void;
  onNewApplication: () => void;
  onOpenApplication: (application: ApplicationSession) => void;
  applicationStatusClass: (status: string) => string;
  applicationHistoryTone: (status: string) => string;
  applicationHistoryLabel: (status: string) => string;
  applicationStatusLabel: (status: string) => string;
  applicationNextActionLabel: (application: ApplicationSession, approvedProfileFactCount: number) => string;
  readinessLabel: (readiness: string) => string;
  applicationHistoryEmptyMessage: (includeArchived: boolean) => string;
  formatDate: (value: string) => string;
};

export function ApplicationListPanel(props: ApplicationListPanelProps) {
  return (
    <section className="list-panel" aria-label="Application sessions">
      <div className="list-header">
        <h3>Applications</h3>
        <button type="button" onClick={props.onNewApplication}>New</button>
      </div>
      <div className="filters">
        <Field label="Search" value={props.applicationSearch} onChange={props.onSearchChange} />
        <Select label="Status" value={props.applicationStatusFilter} options={["All", ...props.applicationStatuses]} onChange={props.onStatusFilterChange} />
        <Select label="Draft/audit" value={props.applicationReadinessFilter} options={props.auditReadinessOptions} onChange={props.onReadinessFilterChange} />
        <label className="checkbox-field">
          <input
            type="checkbox"
            checked={props.includeArchivedApplications}
            onChange={(event) => props.onIncludeArchivedChange(event.target.checked)}
          />
          <span>Include archived</span>
        </label>
      </div>
      <div className="session-list">
        {props.filteredApplications.map((application) => (
          <button
            className={`session ${props.applicationStatusClass(application.status)}${application.id === props.selectedApplicationId ? " active" : ""}`}
            key={application.id}
            type="button"
            onClick={() => props.onOpenApplication(application)}
          >
            <StatusBadge tone={props.applicationHistoryTone(application.status)}>{props.applicationHistoryLabel(application.status)}</StatusBadge>
            <strong>{application.companyName}</strong>
            <span>{application.roleTitle}</span>
            <small>
              {props.applicationStatusLabel(application.status)} - {application.selectedLanguage || application.detectedLanguage || "Language unset"} - Updated {props.formatDate(application.updatedAt)}
            </small>
            <div className="session-badges">
              {application.deadline && <span>Deadline {props.formatDate(application.deadline)}</span>}
              <span className="next-action">Next: {props.applicationNextActionLabel(application, props.approvedProfileFactCount)}</span>
              <span className={application.hasGeneratedDraft ? "ready" : "muted"}>{application.hasGeneratedDraft ? "Draft ready" : "No draft"}</span>
              <span className={`audit-${application.auditReadiness.toLowerCase()}`}>Audit {props.readinessLabel(application.auditReadiness)}</span>
            </div>
          </button>
        ))}
        {props.applications.length === 0 && <p className="empty-state">{props.applicationHistoryEmptyMessage(props.includeArchivedApplications)}</p>}
        {props.applications.length > 0 && props.filteredApplications.length === 0 && <p className="empty-state">No applications match the current search, status, and draft/audit filters.</p>}
      </div>
    </section>
  );
}
