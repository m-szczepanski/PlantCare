export type PlantDueStatus = "NotScheduled" | "Overdue" | "DueToday" | "Upcoming";

export interface Plant {
  id: number;
  nickName: string;
  location: string;
  photoUrl: string | null;
  acquiredDate: string;
  plantProfileId: number | null;
  profileCommonName: string | null;
  customWateringIntervalDays: number | null;
  lastWateredAt: string | null;
  dueStatus: PlantDueStatus;
  wateringIntervalDays: number | null;
  daysUntilDue: number | null;
  nextDueDate: string | null;
  dueMessage: string;
}

export interface WateringLogEntry {
  id: number;
  wateredAt: string;
  note: string | null;
}

export interface PlantProfileOption {
  id: number;
  commonName: string;
  scientificName: string | null;
  defaultWateringIntervalDays: number;
}

export interface PlantInput {
  nickName: string;
  location: string;
  photoUrl?: string | null;
  acquiredDate: string;
  customWateringIntervalDays?: number | null;
  plantProfileId?: number | null;
  lastWateredAt?: string | null;
}
