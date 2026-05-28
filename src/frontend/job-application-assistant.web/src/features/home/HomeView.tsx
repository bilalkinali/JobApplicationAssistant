import type { ReactNode } from "react";
import type { ProfileReadiness } from "../../readiness";
import type { ApplicationSession } from "../shared/types";

type HomeViewProps = {
  profileReadiness: ProfileReadiness;
  applications: ApplicationSession[];
  approvedProfileFactCount: number;
  aiStatusContent: ReactNode;
  onReviewProfile: () => void;
  onNewApplication: () => void;
  onOpenApplication: (application: ApplicationSession) => void;
  getApplicationNextActionLabel: (application: ApplicationSession, approvedProfileFactCount: number) => string;
};

export function HomeView(props: HomeViewProps) {
  return (
    <div className="panel-grid">
      <article className="panel">
        <h3>Profile readiness</h3>
        <p>{props.profileReadiness.contactMessage}</p>
        <p>{props.profileReadiness.evidenceMessage}</p>
        {props.profileReadiness.warnings.length > 0 && (
          <button type="button" onClick={props.onReviewProfile}>
            Review profile setup
          </button>
        )}
      </article>
      <article className="panel">
        <h3>New application</h3>
        <p>Create a saved application workspace for the posting, language, and manual workflow state.</p>
        <button className="primary-action" type="button" onClick={props.onNewApplication}>
          New application
        </button>
      </article>
      <article className="panel">
        <h3>Recent applications</h3>
        <p>{props.applications.length} application session{props.applications.length === 1 ? "" : "s"} saved.</p>
        {props.applications.length > 0 && (
          <div className="recent-next-actions">
            {props.applications.slice(0, 3).map((application) => (
              <button key={application.id} type="button" onClick={() => props.onOpenApplication(application)}>
                <strong>{application.companyName}</strong>
                <span>Next: {props.getApplicationNextActionLabel(application, props.approvedProfileFactCount)}</span>
              </button>
            ))}
          </div>
        )}
      </article>
      <article className="panel">
        <h3>AI status</h3>
        {props.aiStatusContent}
      </article>
    </div>
  );
}
