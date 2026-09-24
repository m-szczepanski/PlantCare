export type PlantDueStatus = "NotScheduled" | "Overdue" | "DueToday" | "Upcoming";

export type CareTaskType = "Watering" | "Fertilizing" | "Repotting";

export type HealthStatus = "Sick" | "Bad" | "Good" | "Excellent";

export interface HealthCheckEntry {
  id: number;
  status: HealthStatus;
  checkedAt: string;
  note: string | null;
}

export type SoilType =
  | "AllPurpose"
  | "CactusMix"
  | "ChunkyBark"
  | "PeatCoco"
  | "SemiHydro"
  | "SelfWatering";

export interface SoilTypeOption {
  type: SoilType;
  wateringIntervalFactor: number;
}

export interface SoilMixOption {
  id: number;
  name: string;
}

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

export interface DiagnosisEntry {
  symptom: string;
  causes: string[];
}

export interface PlantCareTips {
  commonName: string;
  lightRequirement: LightRequirement;
  humidityNotes: string;
  careTips: string;
  diagnosisChecklist: string | null;
}

export interface Plant {
  id: number;
  nickName: string;
  roomId: number | null;
  roomName: string | null;
  photoUrl: string | null;
  potSizeCm: number | null;
  soilType: SoilType | null;
  soilMix: string | null;
  propagatedFrom: string | null;
  notifyEnabled: boolean;
  snoozedUntil: string | null;
  soilWetUntil: string | null;
  acquiredDate: string;
  plantProfileId: number | null;
  profileCommonName: string | null;
  profileToxicToPets: boolean;
  profileToxicToChildren: boolean;
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
  healthStatus: HealthStatus | null;
  lastCheckupAt: string | null;
  checkupDue: boolean;
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

export interface PlantProfile extends PlantProfileOption {
  lightRequirement: LightRequirement;
  humidityNotes: string;
  careTips: string;
  toxicToPets: boolean;
  toxicToChildren: boolean;
  diagnosisChecklist: string | null;
  plantCount: number;
}

export interface PlantProfileInput {
  commonName: string;
  scientificName?: string | null;
  defaultWateringIntervalDays: number;
  lightRequirement: LightRequirement;
  humidityNotes: string;
  careTips: string;
  toxicToPets?: boolean;
  toxicToChildren?: boolean;
  diagnosisChecklist?: string | null;
}

export interface JournalEntry {
  id: number;
  entryDate: string;
  photoUrl: string | null;
  text: string | null;
}

export interface JobRunStatus {
  ranAt: string;
  outcome: string;
  sentDigests: number;
  failed: number;
}

export interface DigestStatus {
  sentAt: string;
  plantCount: number;
  overdueCount: number;
  priority: number;
}

export interface NtfyStatus {
  baseUrl: string;
  publicBaseUrl: string;
  topic: string;
  subscribeUrl: string;
  reachable: boolean;
  latencyMs: number | null;
  error: string | null;
}

export interface StatusInfo {
  nowUtc: string;
  timeZoneId: string;
  wateringCheckCron: string;
  lastJobRun: JobRunStatus | null;
  lastDigest: DigestStatus | null;
  ntfy: NtfyStatus;
  channels: string[];
}

export interface NotificationChannelResult {
  name: string;
  delivered: boolean;
  error: string | null;
}

export interface NotificationTestResult {
  channels: NotificationChannelResult[];
  anyDelivered: boolean;
  total: number;
  delivered: number;
}

export interface WateringCheckResult {
  sentDigests: number;
  skippedDuplicates: number;
  failed: number;
  plantsInDigest: number;
}

export interface ImportResult {
  roomsCreated: number;
  profilesCreated: number;
  plantsCreated: number;
  plantsSkipped: number;
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
  soilType?: SoilType | null;
  soilMix?: string | null;
  propagatedFrom?: string | null;
  notifyEnabled?: boolean;
  acquiredDate: string;
  customWateringIntervalDays?: number | null;
  reduceInWinter?: boolean;
  plantProfileId?: number | null;
  lastWateredAt?: string | null;
}
