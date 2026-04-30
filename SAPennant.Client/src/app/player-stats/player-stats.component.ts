import { Component, Input, OnChanges, SimpleChanges, signal } from '@angular/core';
import { PlayerMatch } from '../models/pennant.models';
import { PennantService } from '../pennant.service';
import { getClubLogo } from '../data/club-logos';
import { LoggingService } from '../logging.service';

@Component({
  selector: 'sa-pennant-player-stats',
  standalone: false,
  templateUrl: './player-stats.component.html',
  styleUrl: './player-stats.component.scss',
})
export class PlayerStatsComponent implements OnChanges {
  @Input() matches: PlayerMatch[] = [];
  @Input() playerName = '';
  @Input() playerClub = '';

  availableYears: number[] = [];
  selectedYears = new Set<number>();
  availablePools: string[] = [];
  selectedPools = new Set<string>();
  shareState = signal<'idle' | 'sharing' | 'copied'>('idle');

  constructor(private logging: LoggingService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['matches'] && this.matches.length > 0) {
      this.availableYears = [...new Set(this.matches.map(m => m.year))].sort((a, b) => b - a);
      this.selectedYears = new Set(this.availableYears);
      this.availablePools = [...new Set(this.matches.map(m => m.pool))].sort();
      this.selectedPools = new Set(this.availablePools);
    }
  }

  toggleYear(year: number): void {
    if (this.selectedYears.has(year)) {
      if (this.selectedYears.size === 1) return;
      this.selectedYears.delete(year);
    } else {
      this.selectedYears.add(year);
    }
    this.selectedYears = new Set(this.selectedYears);
  }

  togglePool(pool: string): void {
    if (this.selectedPools.has(pool)) {
      if (this.selectedPools.size === 1) return;
      this.selectedPools.delete(pool);
    } else {
      this.selectedPools.add(pool);
    }
    this.selectedPools = new Set(this.selectedPools);
  }

  get results(): PlayerMatch[] {
    return this.matches.filter(m =>
      this.selectedYears.has(m.year) &&
      this.selectedPools.has(m.pool)
    );
  }

  get wins(): number { return this.results.filter(m => m.playerWon === true).length; }
  get losses(): number { return this.results.filter(m => m.playerWon === false).length; }
  get halved(): number { return this.results.filter(m => m.playerWon === null).length; }

  get winRate(): number {
    const total = this.wins + this.losses + this.halved;
    return total > 0 ? Math.round((this.wins / total) * 100) : 0;
  }

  get finalsRecord(): string {
    const finals = this.results.filter(m => m.isFinals);
    const finalsWins = finals.filter(m => m.playerWon === true).length;
    return finals.length > 0 ? `${finalsWins}/${finals.length}` : '—';
  }

  get yearRange(): string {
    if (this.availableYears.length === 0) return '';
    const sorted = [...this.availableYears].sort();
    if (sorted.length === 1) return `${sorted[0]}`;
    return `${sorted[0]}–${sorted[sorted.length - 1]}`;
  }

  get allPools(): string {
    return [...new Set(this.matches.map(m => m.pool))].sort().join(', ');
  }

  get groupedResults(): { year: number; isFinals: boolean; matches: PlayerMatch[] }[] {
    const seen = new Map<string, PlayerMatch[]>();
    this.results.forEach(m => {
      const key = `${m.year}-${m.isFinals}`;
      if (!seen.has(key)) seen.set(key, []);
      seen.get(key)!.push(m);
    });
    const groups: { year: number; isFinals: boolean; matches: PlayerMatch[] }[] = [];
    seen.forEach((matches, key) => {
      const [year, isFinals] = key.split('-');
      groups.push({ year: +year, isFinals: isFinals === 'true', matches });
    });
    return groups.sort((a, b) => b.year - a.year || (a.isFinals ? -1 : 1));
  }

  divisionRecord(matches: PlayerMatch[]): string {
    const wins = matches.filter(m => m.playerWon === true).length;
    const losses = matches.filter(m => m.playerWon === false).length;
    const halved = matches.filter(m => m.playerWon === null).length;
    const total = wins + losses + halved;
    const pct = total > 0 ? Math.round(wins / total * 100) : 0;
    return `${wins}W ${losses}L ${halved}H · ${pct}%`;
  }

  formatResult(m: PlayerMatch): string {
    return PennantService.formatResult(m.result, m.playerWon);
  }

  getClubLogoUrl(): string | null {
    return getClubLogo(this.playerClub);
  }

  async sharePlayer(): Promise<void> {
    this.logging.info(`Share player: "${this.playerName}" from ${this.playerClub}`, 'PlayerStatsComponent');

    const name = this.playerName;
    const club = this.playerClub;
    const url = `https://sapennantgolf.com/?player=${encodeURIComponent(name)}`;
    const shareText = `${name} · ${club}\n${this.results.length} matches · ${this.wins}W ${this.losses}L ${this.halved}H · ${this.winRate}% win rate\n\n${url}`;

    const W = 600, H = 280;
    const canvas = document.createElement('canvas');
    const scale = 2;
    canvas.width = W * scale;
    canvas.height = H * scale;

    const ctx = canvas.getContext('2d')!;
    ctx.scale(scale, scale);

    ctx.fillStyle = '#0f1e3d';
    ctx.fillRect(0, 0, W, H);

    const initials = name.split(' ').map((n: string) => n[0]).slice(0, 2).join('').toUpperCase();
    const logoUrl = getClubLogo(club);

    if (logoUrl) {
      await new Promise<void>((resolve) => {
        const img = new Image();
        img.onload = () => {
          ctx.fillStyle = '#ffffff';
          ctx.beginPath();
          ctx.arc(52, 45, 28, 0, Math.PI * 2);
          ctx.fill();
          ctx.save();
          ctx.beginPath();
          ctx.arc(52, 45, 26, 0, Math.PI * 2);
          ctx.clip();
          ctx.imageSmoothingEnabled = true;
          ctx.imageSmoothingQuality = 'high';
          ctx.drawImage(img, 22, 15, 60, 60);
          ctx.restore();
          resolve();
        };
        img.onerror = () => resolve();
        img.src = logoUrl;
      });
    } else {
      ctx.fillStyle = 'rgba(255,255,255,0.15)';
      ctx.beginPath();
      ctx.arc(52, 45, 28, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = '#ffffff';
      ctx.font = 'bold 14px Arial';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(initials, 52, 45);
    }

    ctx.textAlign = 'left';
    ctx.textBaseline = 'alphabetic';
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 22px Arial';
    ctx.fillText(name, 92, 44);

    ctx.fillStyle = 'rgba(255,255,255,0.55)';
    ctx.font = '14px Arial';
    ctx.fillText(`${club} · ${this.yearRange}`, 92, 66);

    ctx.strokeStyle = 'rgba(255,255,255,0.12)';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(24, 90);
    ctx.lineTo(W - 24, 90);
    ctx.stroke();

    const stats = [
      { label: 'PLAYED',   value: String(this.results.length), color: '#ffffff' },
      { label: 'WON',      value: String(this.wins),           color: '#5db83a' },
      { label: 'LOST',     value: String(this.losses),         color: '#e05555' },
      { label: 'HALVED',   value: String(this.halved),         color: '#c47d1a' },
      { label: 'WIN RATE', value: `${this.winRate}%`,          color: '#7eb8f5' },
    ];
    const colW = (W - 48) / stats.length;
    stats.forEach((s, i) => {
      const cx = 24 + colW * i + colW / 2;
      ctx.textAlign = 'center';
      ctx.fillStyle = s.color;
      ctx.font = 'bold 30px Arial';
      ctx.fillText(s.value, cx, 148);
      ctx.fillStyle = 'rgba(255,255,255,0.4)';
      ctx.font = '11px Arial';
      ctx.fillText(s.label, cx, 168);
    });

    ctx.strokeStyle = 'rgba(255,255,255,0.12)';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(24, 185);
    ctx.lineTo(W - 24, 185);
    ctx.stroke();

    ctx.textAlign = 'left';
    ctx.fillStyle = 'rgba(255,255,255,0.3)';
    ctx.font = 'bold 11px Arial';
    ctx.fillText('SA PENNANT GOLF · SOUTH AUSTRALIA', 24, 210);

    ctx.fillStyle = 'rgba(255,255,255,0.4)';
    ctx.font = '11px Arial';
    ctx.fillText(url, 24, 228);

    ctx.textAlign = 'right';
    ctx.fillStyle = 'rgba(255,255,255,0.25)';
    ctx.font = '11px Arial';
    ctx.fillText(this.allPools, W - 24, 228);

    canvas.toBlob(async (blob) => {
      if (!blob || blob.size === 0) return;

      const file = new File([blob], 'pennant-stats.png', { type: 'image/png' });
      const isMobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);

      if (isMobile && navigator.share && navigator.canShare && navigator.canShare({ files: [file] })) {
        try {
          this.shareState.set('sharing');
          await navigator.share({ files: [file], text: shareText });
          this.logging.info(`Share completed (native) for "${name}"`, 'PlayerStatsComponent');
          this.shareState.set('idle');
          return;
        } catch (err: any) {
          if (err?.name !== 'AbortError') {
            this.logging.warn(`Native share failed for "${name}": ${err?.message ?? err}`, 'PlayerStatsComponent');
          }
          this.shareState.set('idle');
        }
      }

      const imageUrl = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = imageUrl;
      a.download = `${name.replace(/\s+/g, '-').toLowerCase()}-pennant-stats.png`;
      a.click();
      URL.revokeObjectURL(imageUrl);
      this.logging.info(`Share downloaded (desktop) for "${name}"`, 'PlayerStatsComponent');

      try {
        await navigator.clipboard.writeText(url);
        this.shareState.set('copied');
        setTimeout(() => this.shareState.set('idle'), 2000);
      } catch {
        this.shareState.set('idle');
      }
    }, 'image/png');
  }
}