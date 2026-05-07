// src/app/core/models/template.models.ts

export enum TemplateCategory {
  RaceResults    = 1,
  BestLoft       = 2,
  AcePigeon      = 3,
  SuperAcePigeon = 4,
  Certificate    = 5
}

export enum TemplateStyle {
  Classic   = 1, Modern    = 2, Elegant   = 3,
  Minimal   = 4, Sporty    = 5, Heritage  = 6,
  Corporate = 7, Vibrant   = 8, Dark      = 9,
  Branded   = 10
}

export enum TemplatePaperSize {
  A4Portrait      = 1, A4Landscape      = 2,
  A3Portrait      = 3, A3Landscape      = 4,
  LetterPortrait  = 5, LetterLandscape  = 6
}

export enum PrintJobStatus {
  Pending = 1, Rendering = 2, Complete = 3, Failed = 4
}

export interface PrintTemplate {
  id: string;
  name: string;
  description: string;
  category: TemplateCategory;
  categoryName: string;
  style: TemplateStyle;
  styleName: string;
  paperSize: TemplatePaperSize;
  paperSizeName: string;
  colourScheme: number;
  primaryColour: string;
  secondaryColour: string;
  thumbnailUrl: string;
  maxRows: number;
  isMultiPage: boolean;
  isSystem: boolean;
  sortOrder: number;
  variableSchemaJson: string;
}

export interface RenderTemplateRequest {
  templateId: string;
  category: TemplateCategory;
  raceId?: string;
  programmeId?: string;
  raceResultId?: string;
  certificateRecipientName?: string;
  certificateRank?: string;
  certificateAchievement?: string;
}

export interface PrintJob {
  id: string;
  templateId: string;
  templateName: string;
  category: TemplateCategory;
  status: PrintJobStatus;
  pdfUrl?: string;
  fileSizeBytes?: number;
  createdAt: string;
  completedAt?: string;
  generatedByUserName: string;
}
