export type PlantDueStatus = "NotScheduled" | "Overdue" | "DueToday" | "Upcoming";

export type LightRequirement = "Low" | "Medium" | "Bright" | "DirectSun";

export type RoomOrientation = "North" | "East" | "South" | "West";

export interface Room {
  id: number;
  name: string;
  orientation: RoomOrientation | null;
  plantCount: number;
}

export interface RoomInput {
  name: string;
  orientation?: RoomOrientation | null;
}

export interface PlantCareTips {
  commonName: string;
  lightRequirement: LightRequirement;
  humidityNotes: string;
  careTips: string;
}

export interface Plant {
  id: number;
  nickName: string;
  roomId: number | null;
  roomName: string | null;
  photoUrl: string | null;
  acquiredDate: string;
  plantProfileId: number | null;
  profileCommonName: string | null;
  careTips: PlantCareTips | null;
  customWateringIntervalDays: number | null;
  lastWateredAt: string | null;
  dueStatus: PlantDueStatus;
  wateringIntervalDays: number | null;
  daysUntilDue: number | null;
  nextDueDate: string | null;
  dueMessage: string;
}

export interface Dashboard {
  overdue: Plant[];
  dueToday: Plant[];
  upcoming: Plant[];
}

export interface SpeciesCount {
  plantProfileId: number;
  commonName: string;
  plantCount: number;
}

export interface NeglectedPlant {
  plantId: number;
  nickName: string;
  daysSinceLastWatering: number;
}

export interface PlantStreak {
  plantId: number;
  nickName: string;
  consecutiveOnTimeWaterings: number;
}

export interface MonthlyCount {
  month: string;
  count: number;
}

export interface Insights {
  totalPlants: number;
  scheduledPlants: number;
  unscheduledPlants: number;
  speciesCount: number;
  species: SpeciesCount[];
  mostNeglected: NeglectedPlant[];
  adherenceWindowDays: number;
  adherencePercent: number;
  expectedWateringsInWindow: number;
  actualWateringsInWindow: number;
  streaks: PlantStreak[];
  monthlyWaterings: MonthlyCount[];
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
  roomId?: number | null;
  photoUrl?: string | null;
  acquiredDate: string;
  customWateringIntervalDays?: number | null;
  plantProfileId?: number | null;
  lastWateredAt?: string | null;
}
