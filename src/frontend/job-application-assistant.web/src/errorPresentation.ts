export type ApiError = {
  code: string;
  message: string;
  details?: Record<string, string[]>;
};

export type ErrorPresentation = {
  title: string;
  message: string;
  details?: string[];
};

export function formatError(error: unknown): ErrorPresentation {
  const apiError = error as ApiError;
  if (!apiError?.message) {
    return plainError("Request failed", error instanceof Error ? error.message : "The API request failed.");
  }

  const details = apiError.details ? formatDetails(apiError.details) : [];
  const aiProviderMessages = apiError.details?.AiProvider;
  if (aiProviderMessages?.length) {
    const providerMessage = aiProviderMessages.join(" ");
    if (providerMessage.toLowerCase().includes("unavailable")) {
      return plainError(
        "AI provider unavailable",
        `${providerMessage} Check AI settings, then run diagnostics. Existing workflow state was kept.`,
        ["Category: provider unavailable", ...details, ...apiCodeDetail(apiError)]
      );
    }

    if (isInvalidAiOutputMessage(providerMessage)) {
      return plainError(
        "AI output could not be used",
        `${providerMessage} Existing workflow state was kept.`,
        ["Category: invalid AI output", ...details, ...apiCodeDetail(apiError)]
      );
    }

    return plainError(
      "AI provider failed",
      `${providerMessage} Existing workflow state was kept.`,
      ["Category: provider failure", ...details, ...apiCodeDetail(apiError)]
    );
  }

  if (apiError.code === "validation_error") {
    return plainError(
      "Validation blocker",
      apiError.message,
      [...details, ...apiCodeDetail(apiError)]
    );
  }

  return plainError(apiError.code === "not_found" ? "Not found" : "Request failed", apiError.message, [
    ...details,
    ...apiCodeDetail(apiError)
  ]);
}

export function plainError(title: string, message: string, details: string[] = []): ErrorPresentation {
  return {
    title,
    message,
    details: details.length > 0 ? details : undefined
  };
}

export function technicalDetails(error: unknown): string[] {
  if (error instanceof Error) {
    return [error.message];
  }

  if (typeof error === "string") {
    return [error];
  }

  return [];
}

function formatDetails(details: Record<string, string[]>): string[] {
  return Object.entries(details).flatMap(([field, messages]) =>
    messages.map((message) => `${field}: ${message}`)
  );
}

function apiCodeDetail(error: ApiError): string[] {
  return error.code ? [`API code: ${error.code}`] : [];
}

function isInvalidAiOutputMessage(message: string): boolean {
  const normalized = message.toLowerCase();

  return (
    normalized.includes("invalid") ||
    normalized.includes("malformed") ||
    normalized.includes("empty response") ||
    normalized.includes("incomplete") ||
    normalized.includes("duplicate")
  );
}
