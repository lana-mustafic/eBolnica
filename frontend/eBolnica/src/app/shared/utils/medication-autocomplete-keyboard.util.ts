export type MedicationAutocompleteKeyboardAction =
  | { type: 'none' }
  | { type: 'moveHighlight'; nextIndex: number }
  | { type: 'selectHighlighted'; index: number }
  | { type: 'closeDropdown' };

export interface MedicationAutocompleteKeyboardState {
  showDropdown: boolean;
  suggestionsCount: number;
  highlightIndex: number;
}

export interface MedicationAutocompleteKeyboardResult {
  action: MedicationAutocompleteKeyboardAction;
}

/**
 * Compute the next highlighted suggestion index for ArrowUp/ArrowDown navigation.
 */
export function moveAutocompleteHighlightIndex(
  currentIndex: number,
  suggestionsCount: number,
  delta: number
): number {
  if (suggestionsCount <= 0) {
    return -1;
  }

  const maxIndex = suggestionsCount - 1;

  if (currentIndex < 0 && delta > 0) {
    return 0;
  }

  return Math.min(maxIndex, Math.max(-1, currentIndex + delta));
}

/**
 * Map keyboard input to autocomplete actions for ArrowDown, ArrowUp, Enter, and Escape.
 */
export function handleMedicationAutocompleteKeydown(
  key: string,
  state: MedicationAutocompleteKeyboardState
): MedicationAutocompleteKeyboardResult {
  if (!state.showDropdown) {
    return { action: { type: 'none' } };
  }

  switch (key) {
    case 'ArrowDown':
      return {
        action: {
          type: 'moveHighlight',
          nextIndex: moveAutocompleteHighlightIndex(
            state.highlightIndex,
            state.suggestionsCount,
            1
          )
        }
      };
    case 'ArrowUp':
      return {
        action: {
          type: 'moveHighlight',
          nextIndex: moveAutocompleteHighlightIndex(
            state.highlightIndex,
            state.suggestionsCount,
            -1
          )
        }
      };
    case 'Enter':
      if (
        state.highlightIndex >= 0 &&
        state.highlightIndex < state.suggestionsCount
      ) {
        return {
          action: {
            type: 'selectHighlighted',
            index: state.highlightIndex
          }
        };
      }
      return { action: { type: 'none' } };
    case 'Escape':
      return { action: { type: 'closeDropdown' } };
    default:
      return { action: { type: 'none' } };
  }
}
