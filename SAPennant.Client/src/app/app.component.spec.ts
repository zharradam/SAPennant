import { describe, expect, it } from 'vitest';
import { tabFromPath } from './app.component';

describe('tabFromPath', () => {
  it('maps the root to no tab (defaults to team)', () => {
    expect(tabFromPath('/')).toBeNull();
  });

  it('ignores a trailing slash', () => {
    expect(tabFromPath('/search/')).toBe('search');
  });

  it('treats index.html as the root', () => {
    expect(tabFromPath('/index.html')).toBeNull();
  });

  it('recognises the admin tab', () => {
    expect(tabFromPath('/admin')).toBe('admin');
  });

  it('returns null for unknown paths', () => {
    expect(tabFromPath('/nonsense')).toBeNull();
  });
});
