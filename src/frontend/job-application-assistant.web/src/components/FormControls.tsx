type FieldProps = {
  label: string;
  type?: string;
  required?: boolean;
  value: string;
  onChange: (value: string) => void;
};

export function Field(props: FieldProps) {
  return (
    <label className="field">
      <span>{props.label}</span>
      <input
        type={props.type ?? "text"}
        required={props.required}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      />
    </label>
  );
}

type SelectProps = {
  label: string;
  required?: boolean;
  value: string;
  options: string[];
  onChange: (value: string) => void;
};

export function Select(props: SelectProps) {
  return (
    <label className="field">
      <span>{props.label}</span>
      <select
        required={props.required}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      >
        {props.options.map((option) => (
          <option key={option} value={option}>{option}</option>
        ))}
      </select>
    </label>
  );
}

type TextareaProps = {
  label: string;
  required?: boolean;
  value: string;
  onChange: (value: string) => void;
};

export function Textarea(props: TextareaProps) {
  return (
    <label className="field wide">
      <span>{props.label}</span>
      <textarea required={props.required} value={props.value} onChange={(event) => props.onChange(event.target.value)} />
    </label>
  );
}
