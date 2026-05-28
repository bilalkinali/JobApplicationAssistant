import type { ErrorPresentation } from "../errorPresentation";
import type { InlineFeedback } from "../features/shared/types";

export function ErrorMessage(props: { error: ErrorPresentation; compact?: boolean }) {
  return (
    <div className={props.compact ? "message error compact" : "message error"} role="alert">
      <strong>{props.error.title}</strong>
      <p>{props.error.message}</p>
      {props.error.details && props.error.details.length > 0 && (
        <details>
          <summary>Technical details</summary>
          <ul>
            {props.error.details.map((detail, index) => (
              <li key={`${detail}-${index}`}>{detail}</li>
            ))}
          </ul>
        </details>
      )}
    </div>
  );
}

export function InlineFeedbackMessage(props: { feedback: InlineFeedback }) {
  return (
    <div className={`workflow-note ${props.feedback.tone}`} role={props.feedback.tone === "error" ? "alert" : "status"}>
      <strong>{props.feedback.title}</strong>
      <p>{props.feedback.message}</p>
      {props.feedback.details && props.feedback.details.length > 0 && (
        <details>
          <summary>Technical details</summary>
          <ul>
            {props.feedback.details.map((detail, index) => (
              <li key={`${detail}-${index}`}>{detail}</li>
            ))}
          </ul>
        </details>
      )}
    </div>
  );
}
