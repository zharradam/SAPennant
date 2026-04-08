import { Pipe, PipeTransform } from '@angular/core';
import { PlayerMatch } from './models/pennant.models';

@Pipe({
  name: 'poolCount',
  standalone: false,
})
export class PoolCountPipe implements PipeTransform {
  transform(matches: PlayerMatch[], pool: string): number {
    return matches.filter(m => m.pool === pool).length;
  }
}