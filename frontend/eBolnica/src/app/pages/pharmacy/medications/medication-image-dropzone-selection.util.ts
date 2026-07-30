import { filesFromInput } from './medication-image-dropzone-file-picker.util';

/** Default max files per upload batch (matches user story limit). */
export const MEDICATION_IMAGE_MAX_FILES = 5;

export interface NormalizeSelectedFilesOptions {
  multiple: boolean;
  maxFiles: number;
}

export interface NormalizedFileSelection {
  files: File[];
  totalProvided: number;
  wasLimited: boolean;
}

export interface SelectionLimitedEvent {
  selected: number;
  provided: number;
  maxFiles: number;
}

export function normalizeSelectedFiles(
  fileList: FileList | null | undefined,
  options: NormalizeSelectedFilesOptions
): NormalizedFileSelection {
  const allFiles = filesFromInput(fileList);

  if (allFiles.length === 0) {
    return { files: [], totalProvided: 0, wasLimited: false };
  }

  const limit = options.multiple ? Math.max(1, options.maxFiles) : 1;
  const files = allFiles.slice(0, limit);

  return {
    files,
    totalProvided: allFiles.length,
    wasLimited: allFiles.length > limit
  };
}

export function buildSelectionLimitedEvent(
  selection: NormalizedFileSelection,
  maxFiles: number
): SelectionLimitedEvent {
  return {
    selected: selection.files.length,
    provided: selection.totalProvided,
    maxFiles
  };
}
