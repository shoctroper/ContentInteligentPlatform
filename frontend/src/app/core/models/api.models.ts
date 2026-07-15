export interface ProfileDto {
  id: string;
  slug: string;
  name: string;
  parentSlug: string | null;
  version: number;
}

export interface GenerationSummaryDto {
  id: string;
  profileSlug: string;
  createdAt: string;
  rating: number | null;
}

export interface GenerationDetailDto {
  id: string;
  profileSlug: string;
  providerName: string;
  resultMarkdown: string;
  resultJson: string;
  confidence: number;
  missingInformation: string | null;
  tokensInput: number;
  tokensOutput: number;
  costUsd: number;
  rating: number | null;
  createdAt: string;
}

export type OutputFormat = 'TikTok' | 'YouTubeShorts' | 'InstagramReel' | 'Podcast';

export interface GenerateScriptRequest {
  sourceText: string;
  profileSlug: string;
  outputFormat: OutputFormat;
}

export interface RateGenerationRequest {
  rating: number;
  comments?: string | null;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
}
