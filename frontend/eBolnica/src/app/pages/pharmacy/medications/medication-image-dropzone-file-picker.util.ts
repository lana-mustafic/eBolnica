let dropzoneInputCounter = 0;

/** Creates a unique id for associating a browse label with a hidden file input. */
export function createDropzoneFileInputId(prefix = 'medication-image-file-input'): string {
  dropzoneInputCounter += 1;
  return `${prefix}-${dropzoneInputCounter}`;
}

/** Converts a native FileList into a File array. */
export function filesFromInput(fileList: FileList | null | undefined): File[] {
  if (!fileList?.length) return [];
  return Array.from(fileList);
}

/** Clears the native input so the same file can be selected again. */
export function resetNativeFileInput(input: HTMLInputElement | null | undefined): void {
  if (input) {
    input.value = '';
  }
}
