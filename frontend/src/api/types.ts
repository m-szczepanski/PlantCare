export type PlantDueStatus = "NotScheduled" | "Overdue" | "DueToday" | "Upcoming";

export type CareTaskType = "Watering" | "Fertilizing" | "Repotting";

export interface CareTask {
  id: number;
  type: CareTaskType;
  intervalDays: number | null;
  lastDoneAt: string | null;
  reduceInWinter: boolean | null;
  dueStatus: PlantDueStatus;
  daysUntilDue: number | null;
  nextDueDate: string | null;
  dueMessage: string;
  inWinterNow: boolean;
  hint: string | null;
}

export type LightRequirement = "Low" | "Medium" | "Bright" | "DirectSun";

export type RoomOrientation = "North" | "East" | "South" | "West";

export type HumidityLevel = "Low" | "Medium" | "High";

export type RoomLightMatch =
  | "Good"
  | "SlightlyTooBright"
  | "SlightlyTooDark"
  | "MuchTooBright"
  | "MuchTooDark";

export interface Room {
  id: number;
  name: string;
  orientation: RoomOrientation | null;
  lightExposure: LightRequirement | null;
  humidity: HumidityLevel | null;
  temperatureCelsius: number | null;
  plantCount: number;
}

export interface RoomInput {
  name: string;
  orientation?: RoomOrientation | null;
  lightExposure?: LightRequirement | null;
  humidity?: HumidityLevel | null;
  temperatureCelsius?: number | null;
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
  potSizeCm: number | null;
  soilMix: string | null;
  propagatedFrom: string | null;
  acquiredDate: string;
  plantProfileId: number | null;
  profileCommonName: string | null;
  careTips: PlantCareTips | null;
  customWateringIntervalDays: number | null;
  reduceInWinter: boolean | null;
  lastWateredAt: string | null;
  dueStatus: PlantDueStatus;
  wateringIntervalDays: number | null;
  daysUntilDue: number | null;
  nextDueDate: string | null;
  dueMessage: string;
  roomLightMatch: RoomLightMatch | null;
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

export type WateringMethod = "Tap" | "Filtered" | "Rainwater";

export interface WateringLogEntry {
  id: number;
  wateredAt: string;
  note: string | null;
  amountMilliliters: number | null;
  method: WateringMethod | null;
}

export interface WaterDetails {
  note?: string;
  amountMilliliters?: number;
  method?: WateringMethod;
}

export interface PlantProfileOption {
  id: number;
  commonName: string;
  scientificName: string | null;
  defaultWateringIntervalDays: number;
}

export interface PlantNote {
  id: number;
  createdAt: string;
  text: string;
}

export interface PlantInput {
  nickName: string;
  roomId?: number | null;
  photoUrl?: string | null;
  potSizeCm?: number | null;
  soilMix?: string | null;
  propagatedFrom?: string | null;
  acquiredDate: string;
  customWateringIntervalDays?: number | null;
  reduceInWinter?: boolean;
  plantProfileId?: number | null;
  lastWateredAt?: string | null;
}
