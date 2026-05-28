import type { FormEvent } from "react";
import { Field, Select, Textarea } from "../../components/FormControls";
import type { ProfileFact, ProfileFactForm } from "../shared/types";

type ProfileFactEditorProps = {
  profileFactForm: ProfileFactForm;
  profileFactStatuses: string[];
  selectedProfileFactId: string | null;
  selectedImportedDraftFact: ProfileFact | undefined;
  selectedImportReviewQueueExists: boolean;
  splitImportedDraftFacts: string;
  onFormChange: (form: ProfileFactForm) => void;
  onSplitChange: (value: string) => void;
  onSave: (event: FormEvent<HTMLFormElement>) => void;
  onApproveImport: () => void;
  onArchiveImport: () => void;
  onSplitImport: () => void;
  onRejectImport: () => void;
  onClear: () => void;
  onDelete: () => void;
};

export function ProfileFactEditor(props: ProfileFactEditorProps) {
  return (
    <form className="form-layout fact-editor" onSubmit={props.onSave}>
      <Field label="Type" required value={props.profileFactForm.type} onChange={(type) => props.onFormChange({ ...props.profileFactForm, type })} />
      <Select label="Status" required value={props.profileFactForm.status} options={props.profileFactStatuses} onChange={(status) => props.onFormChange({ ...props.profileFactForm, status })} />
      <Field label="Title" required value={props.profileFactForm.title} onChange={(title) => props.onFormChange({ ...props.profileFactForm, title })} />
      <Textarea label="Summary" required value={props.profileFactForm.summary} onChange={(summary) => props.onFormChange({ ...props.profileFactForm, summary })} />
      <Textarea label="Fact items JSON" value={props.profileFactForm.factItems} onChange={(factItems) => props.onFormChange({ ...props.profileFactForm, factItems })} />
      <Textarea label="Technologies JSON" value={props.profileFactForm.technologies} onChange={(technologies) => props.onFormChange({ ...props.profileFactForm, technologies })} />
      <Textarea label="Allowed claims JSON" value={props.profileFactForm.allowedClaims} onChange={(allowedClaims) => props.onFormChange({ ...props.profileFactForm, allowedClaims })} />
      <Textarea label="Forbidden claims JSON" value={props.profileFactForm.forbiddenClaims} onChange={(forbiddenClaims) => props.onFormChange({ ...props.profileFactForm, forbiddenClaims })} />
      {props.selectedImportedDraftFact && props.selectedImportReviewQueueExists && (
        <Textarea label="Split into imported draft facts JSON" value={props.splitImportedDraftFacts} onChange={props.onSplitChange} />
      )}
      <div className="form-actions">
        {props.selectedImportedDraftFact && (
          <>
            <button className="primary-action" type="button" onClick={props.onApproveImport}>
              Approve import
            </button>
            <button type="button" onClick={props.onArchiveImport}>
              Archive import
            </button>
            <button type="button" disabled={!props.splitImportedDraftFacts.trim()} onClick={props.onSplitImport}>
              Split import
            </button>
            <button className="danger-action" type="button" onClick={props.onRejectImport}>
              Reject import
            </button>
          </>
        )}
        <button className="primary-action" type="submit">{props.selectedProfileFactId ? "Save fact" : "Create fact"}</button>
        <button type="button" onClick={props.onClear}>Clear</button>
        {props.selectedProfileFactId && (
          <button className="danger-action" type="button" onClick={props.onDelete}>Delete</button>
        )}
      </div>
    </form>
  );
}
