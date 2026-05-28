import type { FormEvent, ReactNode } from "react";
import { Field, Select, Textarea } from "../../components/FormControls";
import type { ApplicationForm, ApplicationSession } from "../shared/types";

type ApplicationEditorProps = {
  selectedApplication: ApplicationSession | undefined;
  selectedApplicationId: string | null;
  applicationForm: ApplicationForm;
  applicationStatuses: string[];
  workflowBusy: string | null;
  workflowBusyReason: string | null;
  onApplicationFormChange: (form: ApplicationForm) => void;
  onSaveApplication: (event: FormEvent<HTMLFormElement>) => void;
  onStartNewApplication: () => void;
  onDeleteApplication: () => void;
  onMarkApplied: () => void;
  onArchive: () => void;
  disabledTitle: (isDisabled: boolean, reason: string | null | undefined) => string | undefined;
  children: ReactNode;
};

export function ApplicationEditor(props: ApplicationEditorProps) {
  return (
    <form className="form-layout editor-panel" onSubmit={props.onSaveApplication}>
      <div className="section-heading">
        <h3>{props.selectedApplication ? "Application detail" : "New application"}</h3>
        <p>Capture the posting, language, and manual workflow state for this session.</p>
      </div>
      <Field label="Company name" required value={props.applicationForm.companyName} onChange={(companyName) => props.onApplicationFormChange({ ...props.applicationForm, companyName })} />
      <Field label="Role title" required value={props.applicationForm.roleTitle} onChange={(roleTitle) => props.onApplicationFormChange({ ...props.applicationForm, roleTitle })} />
      <Field label="Application URL" type="url" value={props.applicationForm.applicationUrl} onChange={(applicationUrl) => props.onApplicationFormChange({ ...props.applicationForm, applicationUrl })} />
      <Field label="Deadline" type="date" value={props.applicationForm.deadline} onChange={(deadline) => props.onApplicationFormChange({ ...props.applicationForm, deadline })} />
      <Select label="Status" required value={props.applicationForm.status} options={props.applicationStatuses} onChange={(status) => props.onApplicationFormChange({ ...props.applicationForm, status })} />
      <Field label="Detected language" value={props.applicationForm.detectedLanguage} onChange={(detectedLanguage) => props.onApplicationFormChange({ ...props.applicationForm, detectedLanguage })} />
      <Field label="Selected language" value={props.applicationForm.selectedLanguage} onChange={(selectedLanguage) => props.onApplicationFormChange({ ...props.applicationForm, selectedLanguage })} />
      <Textarea label="Job posting text" value={props.applicationForm.jobPostingText} onChange={(jobPostingText) => props.onApplicationFormChange({ ...props.applicationForm, jobPostingText })} />
      <div className="form-actions">
        <button className="primary-action" type="submit">{props.selectedApplication ? "Save application" : "Create application"}</button>
        <button type="button" onClick={props.onStartNewApplication}>Clear</button>
        {props.selectedApplicationId && (
          <button className="danger-action" type="button" onClick={props.onDeleteApplication}>Delete</button>
        )}
      </div>
      {props.selectedApplicationId && (
        <div className="final-status-actions" aria-label="Final application status actions">
          <button
            type="button"
            onClick={props.onMarkApplied}
            disabled={props.selectedApplication?.status === "Applied" || props.workflowBusy !== null}
            title={props.disabledTitle(
              props.selectedApplication?.status === "Applied" || props.workflowBusy !== null,
              props.selectedApplication?.status === "Applied" ? "This application is already marked applied." : props.workflowBusyReason
            )}
          >
            Mark applied
          </button>
          <button
            className="danger-action"
            type="button"
            onClick={props.onArchive}
            disabled={props.selectedApplication?.status === "Archived" || props.workflowBusy !== null}
            title={props.disabledTitle(
              props.selectedApplication?.status === "Archived" || props.workflowBusy !== null,
              props.selectedApplication?.status === "Archived" ? "This application is already archived." : props.workflowBusyReason
            )}
          >
            Archive
          </button>
        </div>
      )}

      {props.children}
    </form>
  );
}
