import type { ActionState } from "../../readiness";
import { hasCandidateFitBriefContent } from "../../candidateFitBrief";
import type { CandidateFitBrief } from "../../candidateFitBrief";
import type { ApplicationSession, JobSignalsDocument } from "../shared/types";
import { CandidateFitBriefSummary, SignalColumn } from "../shared/summaries";

type PreparationSectionProps = {
  selectedApplication: ApplicationSession;
  open: boolean;
  summary: string;
  canRunPreparation: boolean;
  workflowBusy: string | null;
  workflowBusyReason: string | null;
  jobAnalysisState: ActionState;
  jobSignals: JobSignalsDocument;
  candidateFitBrief: CandidateFitBrief | null;
  onPrepareApplication: () => void;
  onAnalyzeJob: () => void;
  disabledTitle: (isDisabled: boolean, reason: string | null | undefined) => string | undefined;
};

export function PreparationSection(props: PreparationSectionProps) {
  return (
    <details className="context-disclosure preparation-context" open={props.open}>
      <summary>
        <span>Step 1. Prepare automatically</span>
        <small>{props.summary}</small>
      </summary>
      {props.selectedApplication.preparationStatus !== "NotStarted" && (
        <div className="workflow-step">
          <div>
            <h4>Preparation</h4>
            <p>Runs job analysis, candidate fit, evidence matching, and strategy in one pass.</p>
          </div>
          <button
            className="secondary-workflow-action"
            type="button"
            onClick={props.onPrepareApplication}
            disabled={!props.canRunPreparation || props.workflowBusy !== null}
            title={props.disabledTitle(
              !props.canRunPreparation || props.workflowBusy !== null,
              props.workflowBusyReason ?? "Save a posting and approve at least one profile fact before preparing."
            )}
          >
            {props.workflowBusy === "prepare" ? "Preparing..." : "Re-run preparation"}
          </button>
        </div>
      )}

      <div className="workflow-step">
        <div>
          <h4>Manual: job analysis only</h4>
          <p>{props.jobAnalysisState.message}</p>
        </div>
        <button
          className="secondary-workflow-action"
          type="button"
          onClick={props.onAnalyzeJob}
          disabled={!props.jobAnalysisState.canRun || props.workflowBusy !== null}
          title={props.disabledTitle(!props.jobAnalysisState.canRun || props.workflowBusy !== null, props.workflowBusyReason ?? props.jobAnalysisState.message)}
        >
          {props.workflowBusy === "analysis" ? "Analyzing..." : "Analyze job"}
        </button>
      </div>

      {props.jobSignals.signals.length > 0 ? (
        <div className="signal-grid">
          <SignalColumn title="Required skills" values={props.jobSignals.requiredSkills} />
          <SignalColumn title="Preferred skills" values={props.jobSignals.preferredSkills} />
          <SignalColumn title="Responsibilities" values={props.jobSignals.responsibilities} />
        </div>
      ) : (
        <p className="empty-state compact">No analysis results yet.</p>
      )}

      {hasCandidateFitBriefContent(props.candidateFitBrief) && (
        <CandidateFitBriefSummary brief={props.candidateFitBrief} />
      )}
    </details>
  );
}
