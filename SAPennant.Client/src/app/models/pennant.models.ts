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

export interface HandicapPlayer {
  playerName: string;
  club: string;
  lowestHandicap: number;
  currentHandicap: number;
  dataPoints: number;
}

export interface HandicapDataPoint {
  date: string;
  sortDate: string;
  handicap: number;
  opponent: string;
  result: string;
  pool: string;
  year: number;
}

export interface UsageCount { name: string; count: number; }
export interface UsageDay { day: string; visits: number; visitors: number; events: number; }
export interface UsageSummary {
  visits: number;
  visitors: number;
  events: number;
  newVisitors: number;
  mobileVisits: number;
  knownDeviceVisits: number;
  previous: { visits: number; visitors: number; events: number } | null;
  days: UsageDay[];
  byHour: number[];
  byWeekday: number[];
  byEvent: UsageCount[];
  topSearches: UsageCount[];
  topMisses: UsageCount[];
  topPlayers: UsageCount[];
  topClubs: UsageCount[];
  topReferrers: UsageCount[];
}