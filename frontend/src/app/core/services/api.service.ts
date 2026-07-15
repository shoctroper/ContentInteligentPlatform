import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  GenerateScriptRequest,
  GenerationDetailDto,
  GenerationSummaryDto,
  ProfileDto,
  RateGenerationRequest
} from '../models/api.models';

/**
 * Cliente HTTP tipado contra docs/architecture/openapi.yaml.
 * Escrito a mano por velocidad (ADR-006); considerar autogenerarlo si el contrato cambia seguido.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getProfiles(): Observable<ProfileDto[]> {
    return this.http.get<ProfileDto[]>(`${this.baseUrl}/api/profiles`);
  }

  getGenerationHistory(profileSlug?: string): Observable<GenerationSummaryDto[]> {
    const params: Record<string, string> = profileSlug ? { profileSlug } : {};
    return this.http.get<GenerationSummaryDto[]>(`${this.baseUrl}/api/generations`, { params });
  }

  getGenerationById(id: string): Observable<GenerationDetailDto> {
    return this.http.get<GenerationDetailDto>(`${this.baseUrl}/api/generations/${id}`);
  }

  generateScript(request: GenerateScriptRequest): Observable<GenerationDetailDto> {
    return this.http.post<GenerationDetailDto>(`${this.baseUrl}/api/generations`, request);
  }

  rateGeneration(id: string, request: RateGenerationRequest): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/api/generations/${id}/rating`, request);
  }
}
