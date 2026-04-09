export interface Season {
  year: number;
  regularId: number;
  finalsId?: number;
}

export const SEASONS: Season[] = [
  { year: 2026, regularId: 2334 },
  { year: 2025, regularId: 2143, finalsId: 2145 },
  { year: 2024, regularId: 1989, finalsId: 1993 },
  { year: 2023, regularId: 1785, finalsId: 1911 },
  { year: 2022, regularId: 1589, finalsId: 1664 },
  { year: 2021, regularId: 1343, finalsId: 1416 },
];

export interface Pool {
  name: string;
  competitionId: number;
  division: string;
}

export interface PlayerMatch {
  id: number;
  year: number;
  isFinals: boolean;
  division: string;
  pool: string;
  round: string;
  date: string;
  sortDate: string;
  homeClub: string;
  awayClub: string;
  playerName: string;
  opponentName: string;
  playerClub: string;
  opponentClub: string;
  playerHandicap: string | null;
  opponentHandicap: string | null;
  venue: string | null;
  result: string;
  playerWon: boolean | null;
  format: string;
}

export interface ClubPlayer {
  playerName: string;
  club: string;
  year: number;
  pool: string;
  played: number;
  wins: number;
  losses: number;
  halved: number;
  winRate: number;
}