import { PoolCountPipe } from './pool-count.pipe';

describe('PoolCountPipe', () => {
  it('create an instance', () => {
    const pipe = new PoolCountPipe();
    expect(pipe).toBeTruthy();
  });
});
