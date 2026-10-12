import { describe, expect, it } from 'vitest';
import { PennantService } from './pennant.service';

describe('PennantService.formatResult', () => {
  it('shows a win by holes as "N up"', () => {
    expect(PennantService.formatResult('3 Holes', true)).toBe('3 up');
  });

  it('shows a loss by holes as "N down"', () => {
    expect(PennantService.formatResult('3 Holes', false)).toBe('3 down');
  });

  it('shows all square as Halved', () => {
    expect(PennantService.formatResult('A/S', null)).toBe('Halved');
  });

  it('passes other results through untouched', () => {
    expect(PennantService.formatResult('Forfeit', true)).toBe('Forfeit');
  });
});
