import { Pipe, PipeTransform } from '@angular/core';
import { PlayerMatch } from './models/pennant.models';

@Pipe({
  name: 'yearCount',
  standalone: false,
})
export class YearCountPipe implements PipeTransform {
  transform(matches: PlayerMatch[], year: number): number {
    return matches.filter(m => m.year === year).length;
  }
}