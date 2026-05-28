import type { FormEvent, RefObject } from "react";
import type { InlineFeedback } from "../shared/types";

type ProfileImportPanelProps = {
  isFakeAiProvider: boolean;
  busy: boolean;
  feedback: InlineFeedback | null;
  fileInputRef: RefObject<HTMLInputElement | null>;
  onFileChange: (file: File | null) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
};

export function ProfileImportPanel(props: ProfileImportPanelProps) {
  return (
    <form className="profile-import-panel" onSubmit={props.onSubmit}>
      <div className="section-heading">
        <h4>Assisted CV import</h4>
        <p>Upload a PDF CV to draft profile facts, review the grouped queue, and approve evidence for saved applications.</p>
      </div>
      {props.isFakeAiProvider && (
        <p className="workflow-note warning">Fake AI mode is active. Import will use deterministic demo/test extraction behavior.</p>
      )}
      <label className="file-field">
        <span>PDF CV</span>
        <input
          accept="application/pdf,.pdf"
          ref={props.fileInputRef}
          type="file"
          onChange={(event) => props.onFileChange(event.target.files?.[0] ?? null)}
        />
      </label>
      <div className="form-actions">
        <button className="primary-action" type="submit" disabled={props.busy}>
          {props.busy ? "Importing..." : "Import PDF CV"}
        </button>
      </div>
      {props.feedback && (
        <div className={`message compact ${props.feedback.tone}`} role="status">
          <strong>{props.feedback.title}</strong>
          <p>{props.feedback.message}</p>
        </div>
      )}
    </form>
  );
}
