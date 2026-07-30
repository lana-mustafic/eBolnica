import {
  handleMedicationAutocompleteKeydown,
  moveAutocompleteHighlightIndex
} from './medication-autocomplete-keyboard.util';

describe('moveAutocompleteHighlightIndex', () => {
  it('returns -1 when there are no suggestions', () => {
    expect(moveAutocompleteHighlightIndex(-1, 0, 1)).toBe(-1);
  });

  it('selects the first suggestion on ArrowDown from no highlight', () => {
    expect(moveAutocompleteHighlightIndex(-1, 3, 1)).toBe(0);
  });

  it('moves down and up within bounds', () => {
    expect(moveAutocompleteHighlightIndex(0, 3, 1)).toBe(1);
    expect(moveAutocompleteHighlightIndex(1, 3, -1)).toBe(0);
    expect(moveAutocompleteHighlightIndex(0, 3, -1)).toBe(-1);
    expect(moveAutocompleteHighlightIndex(2, 3, 1)).toBe(2);
  });
});

describe('handleMedicationAutocompleteKeydown', () => {
  const openState = {
    showDropdown: true,
    suggestionsCount: 3,
    highlightIndex: -1
  };

  it('ignores keys when dropdown is closed', () => {
    const result = handleMedicationAutocompleteKeydown('ArrowDown', {
      ...openState,
      showDropdown: false
    });

    expect(result.action).toEqual({ type: 'none' });
  });

  it('moves highlight on ArrowDown and ArrowUp', () => {
    expect(handleMedicationAutocompleteKeydown('ArrowDown', openState).action).toEqual({
      type: 'moveHighlight',
      nextIndex: 0
    });

    expect(
      handleMedicationAutocompleteKeydown('ArrowUp', {
        ...openState,
        highlightIndex: 1
      }).action
    ).toEqual({
      type: 'moveHighlight',
      nextIndex: 0
    });
  });

  it('selects highlighted suggestion on Enter', () => {
    const result = handleMedicationAutocompleteKeydown('Enter', {
      ...openState,
      highlightIndex: 2
    });

    expect(result.action).toEqual({
      type: 'selectHighlighted',
      index: 2
    });
  });

  it('does not select on Enter when nothing is highlighted', () => {
    const result = handleMedicationAutocompleteKeydown('Enter', openState);

    expect(result.action).toEqual({ type: 'none' });
  });

  it('closes dropdown on Escape', () => {
    const result = handleMedicationAutocompleteKeydown('Escape', {
      ...openState,
      highlightIndex: 1
    });

    expect(result.action).toEqual({ type: 'closeDropdown' });
  });
});
