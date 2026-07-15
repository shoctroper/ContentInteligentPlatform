import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { GenerationDetailDto, ProfileDto } from '../models/api.models';
import { ApiService } from './api.service';

describe('ApiService', () => {
  let service: ApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApiService]
    });
    service = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getProfiles hace GET a /api/profiles y retorna la lista', () => {
    const mock: ProfileDto[] = [{ id: '1', slug: 'periodistico', name: 'Periodístico', parentSlug: null, version: 1 }];

    service.getProfiles().subscribe((profiles) => {
      expect(profiles).toEqual(mock);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/profiles`);
    expect(req.request.method).toBe('GET');
    req.flush(mock);
  });

  it('generateScript hace POST a /api/generations con el body correcto', () => {
    const mockResponse = { id: 'gen-1', profileSlug: 'periodistico' } as GenerationDetailDto;

    service.generateScript({ sourceText: 'texto', profileSlug: 'periodistico', outputFormat: 'TikTok' })
      .subscribe((result) => {
        expect(result).toEqual(mockResponse);
      });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/generations`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ sourceText: 'texto', profileSlug: 'periodistico', outputFormat: 'TikTok' });
    req.flush(mockResponse);
  });

  it('getGenerationHistory sin filtro no envía profileSlug como query param', () => {
    service.getGenerationHistory().subscribe();

    const req = httpMock.expectOne((r) => r.url === `${environment.apiBaseUrl}/api/generations`);
    expect(req.request.params.has('profileSlug')).toBeFalse();
    req.flush([]);
  });

  it('getGenerationHistory con filtro envía profileSlug como query param', () => {
    service.getGenerationHistory('periodistico').subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === `${environment.apiBaseUrl}/api/generations` && r.params.get('profileSlug') === 'periodistico'
    );
    req.flush([]);
  });

  it('rateGeneration hace PATCH a /api/generations/{id}/rating', () => {
    service.rateGeneration('gen-1', { rating: 5, comments: 'Excelente' }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/generations/gen-1/rating`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ rating: 5, comments: 'Excelente' });
    req.flush(null);
  });
});
