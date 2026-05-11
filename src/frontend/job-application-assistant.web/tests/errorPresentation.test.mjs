import assert from "node:assert/strict";
import test from "node:test";
import { formatError, plainError, technicalDetails } from "../dist-test/errorPresentation.js";

test("AI provider unavailable errors are distinct from validation blockers", () => {
  const error = formatError({
    code: "validation_error",
    message: "The request payload is invalid.",
    details: {
      AiProvider: ["Ollama endpoint is unavailable."]
    }
  });

  assert.equal(error.title, "AI provider unavailable");
  assert.match(error.message, /run diagnostics/i);
  assert.match(error.message, /Existing workflow state was kept/);
  assert.ok(error.details?.includes("Category: provider unavailable"));
});

test("invalid AI output is plain and does not imply corrupted saved state", () => {
  const error = formatError({
    code: "validation_error",
    message: "The request payload is invalid.",
    details: {
      AiProvider: ["Ollama returned structurally invalid draft generation JSON."]
    }
  });

  assert.equal(error.title, "AI output could not be used");
  assert.match(error.message, /Existing workflow state was kept/);
  assert.doesNotMatch(error.message, /corrupt/i);
  assert.ok(error.details?.includes("Category: invalid AI output"));
});

test("validation details stay expandable instead of being folded into the plain message", () => {
  const error = formatError({
    code: "validation_error",
    message: "The request payload is invalid.",
    details: {
      JobPostingText: ["Job posting text is required before analysis."]
    }
  });

  assert.equal(error.title, "Validation blocker");
  assert.equal(error.message, "The request payload is invalid.");
  assert.deepEqual(error.details, [
    "JobPostingText: Job posting text is required before analysis.",
    "API code: validation_error"
  ]);
});

test("plain and technical errors support copy and export failures", () => {
  assert.deepEqual(plainError("Export blocked", "Save draft edits before downloading TXT or DOCX."), {
    title: "Export blocked",
    message: "Save draft edits before downloading TXT or DOCX.",
    details: undefined
  });
  assert.deepEqual(technicalDetails(new Error("Clipboard permission denied.")), [
    "Clipboard permission denied."
  ]);
});
