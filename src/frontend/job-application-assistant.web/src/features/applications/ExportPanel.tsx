import type { RefObject } from "react";
import type { AuditExportNotice, CoverLetterExportState } from "../../exportControls";
import type { InlineFeedback } from "../shared/types";
import { InlineFeedbackMessage } from "../../components/FeedbackMessages";

type ExportPanelProps = {
  coverLetterExportState: CoverLetterExportState;
  exportBusy: "txt" | "docx" | null;
  exportBusyReason: string | null;
  exportFeedback: InlineFeedback | null;
  effectiveAuditExportNotice: AuditExportNotice | { tone: "warning"; message: string } | null;
  disabled?: boolean;
  hasGeneratedDraft?: boolean;
  panelRef?: RefObject<HTMLElement | null>;
  onCopyCoverLetter: () => void;
  onDownloadCoverLetter: (format: "txt" | "docx") => void;
  disabledTitle: (isDisabled: boolean, reason: string | null | undefined) => string | undefined;
  claimAuditMessage: (hasGeneratedDraft: boolean) => string;
};

export function ExportPanel(props: ExportPanelProps) {
  if (props.disabled) {
    return (
      <section className="export-panel disabled" ref={props.panelRef}>
        <div className="section-heading">
          <h4>Export cover letter</h4>
          <p>{props.coverLetterExportState.reason ?? props.claimAuditMessage(Boolean(props.hasGeneratedDraft))}</p>
        </div>
        <p className="workflow-note neutral">TXT and DOCX downloads become available after a non-empty generated draft is saved.</p>
        <div className="form-actions">
          <button type="button" disabled title={props.coverLetterExportState.reason ?? "Generate a draft before copying."}>Copy</button>
          <button type="button" disabled title={props.coverLetterExportState.reason ?? "Generate a draft before downloading TXT."}>Download TXT</button>
          <button type="button" disabled title={props.coverLetterExportState.reason ?? "Generate a draft before downloading DOCX."}>Download DOCX</button>
        </div>
      </section>
    );
  }

  return (
    <section className="export-panel" ref={props.panelRef}>
      <div className="section-heading">
        <h4>Export cover letter</h4>
        <p>{props.coverLetterExportState.reason ?? "Copy or download the current saved cover letter exactly as edited."}</p>
      </div>
      <p className="workflow-note info">Copy uses the visible edited text. TXT and DOCX downloads use the current saved draft edits and never regenerate or re-run claim audit.</p>
      {props.effectiveAuditExportNotice && <p className={`workflow-note ${props.effectiveAuditExportNotice.tone}`}>{props.effectiveAuditExportNotice.message}</p>}
      {!props.coverLetterExportState.canCopy && props.coverLetterExportState.canExport && (
        <p className="workflow-note neutral">Clipboard copy is not available in this browser. TXT and DOCX export are still available.</p>
      )}
      {props.exportFeedback && <InlineFeedbackMessage feedback={props.exportFeedback} />}
      <div className="form-actions">
        <button
          type="button"
          onClick={props.onCopyCoverLetter}
          disabled={!props.coverLetterExportState.canCopy || props.exportBusy !== null}
          title={props.disabledTitle(
            !props.coverLetterExportState.canCopy || props.exportBusy !== null,
            props.exportBusyReason ?? props.coverLetterExportState.reason ?? "Clipboard copy is not available in this browser."
          )}
        >
          Copy
        </button>
        <button
          type="button"
          onClick={() => props.onDownloadCoverLetter("txt")}
          disabled={!props.coverLetterExportState.canExport || props.exportBusy !== null}
          title={props.disabledTitle(!props.coverLetterExportState.canExport || props.exportBusy !== null, props.exportBusyReason ?? props.coverLetterExportState.reason)}
        >
          {props.exportBusy === "txt" ? "Downloading..." : "Download TXT"}
        </button>
        <button
          type="button"
          onClick={() => props.onDownloadCoverLetter("docx")}
          disabled={!props.coverLetterExportState.canExport || props.exportBusy !== null}
          title={props.disabledTitle(!props.coverLetterExportState.canExport || props.exportBusy !== null, props.exportBusyReason ?? props.coverLetterExportState.reason)}
        >
          {props.exportBusy === "docx" ? "Downloading..." : "Download DOCX"}
        </button>
      </div>
    </section>
  );
}
