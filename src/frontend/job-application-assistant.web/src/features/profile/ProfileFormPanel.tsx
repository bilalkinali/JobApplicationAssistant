import type { FormEvent } from "react";
import type { ProfileReadiness } from "../../readiness";
import { Field, Textarea } from "../../components/FormControls";
import type { ProfileForm } from "../shared/types";

type ProfileFormPanelProps = {
  profile: ProfileForm;
  profileReadiness: ProfileReadiness;
  onChange: (profile: ProfileForm) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
};

export function ProfileFormPanel(props: ProfileFormPanelProps) {
  return (
    <form className="form-layout panel-form" onSubmit={props.onSubmit}>
      <div className="section-heading">
        <h3>Contact and tone</h3>
        <p>{props.profileReadiness.contactMessage}</p>
      </div>
      {!props.profileReadiness.hasContactDetails && (
        <p className="workflow-note warning">Contact setup is incomplete. You can keep editing applications, but later drafts and exports may miss useful applicant context.</p>
      )}
      <Field label="Full name" required value={props.profile.fullName} onChange={(fullName) => props.onChange({ ...props.profile, fullName })} />
      <Field label="Email" required type="email" value={props.profile.email} onChange={(email) => props.onChange({ ...props.profile, email })} />
      <Field label="Phone" value={props.profile.phone} onChange={(phone) => props.onChange({ ...props.profile, phone })} />
      <Field label="Location" value={props.profile.location} onChange={(location) => props.onChange({ ...props.profile, location })} />
      <Field label="LinkedIn URL" type="url" value={props.profile.linkedInUrl} onChange={(linkedInUrl) => props.onChange({ ...props.profile, linkedInUrl })} />
      <Field label="GitHub URL" type="url" value={props.profile.gitHubUrl} onChange={(gitHubUrl) => props.onChange({ ...props.profile, gitHubUrl })} />
      <Field label="Portfolio URL" type="url" value={props.profile.portfolioUrl} onChange={(portfolioUrl) => props.onChange({ ...props.profile, portfolioUrl })} />
      <Field label="Default language" required value={props.profile.defaultLanguage} onChange={(defaultLanguage) => props.onChange({ ...props.profile, defaultLanguage })} />
      <Textarea label="Danish tone" value={props.profile.danishTone} onChange={(danishTone) => props.onChange({ ...props.profile, danishTone })} />
      <Textarea label="English tone" value={props.profile.englishTone} onChange={(englishTone) => props.onChange({ ...props.profile, englishTone })} />
      <div className="form-actions">
        <button className="primary-action" type="submit">Save profile</button>
      </div>
    </form>
  );
}
