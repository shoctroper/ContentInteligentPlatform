import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { GenerateComponent } from './generate.component';

describe('GenerateComponent', () => {
  let fixture: ComponentFixture<GenerateComponent>;
  let component: GenerateComponent;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GenerateComponent, HttpClientTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(GenerateComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('carga los perfiles al iniciar y preselecciona el primero', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/profiles`);
    req.flush([{ id: '1', slug: 'periodistico', name: 'Periodístico', parentSlug: null, version: 1 }]);

    expect(component.profiles().length).toBe(1);
    expect(component.profileSlug).toBe('periodistico');
  });

  it('submit() envía la generación y guarda el resultado', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/profiles`).flush([]);

    component.sourceText = 'Texto de prueba';
    component.profileSlug = 'periodistico';
    component.submit();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/generations`);
    req.flush({ id: 'gen-1', profileSlug: 'periodistico', resultMarkdown: '# Título', confidence: 0.9 } as any);

    expect(component.result()?.id).toBe('gen-1');
    expect(component.loading()).toBeFalse();
  });

  it('submit() sin texto no dispara request', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/profiles`).flush([]);

    component.sourceText = '';
    component.submit();

    httpMock.expectNone(`${environment.apiBaseUrl}/api/generations`);
  });

  it('un error del backend se muestra en errorMessage', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/profiles`).flush([]);

    component.sourceText = 'Texto';
    component.profileSlug = 'periodistico';
    component.submit();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/generations`);
    req.flush({ detail: 'El perfil no existe.' }, { status: 422, statusText: 'Unprocessable Entity' });

    expect(component.errorMessage()).toBe('El perfil no existe.');
  });
});
