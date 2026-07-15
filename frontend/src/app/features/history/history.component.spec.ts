import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { HistoryComponent } from './history.component';

describe('HistoryComponent', () => {
  let fixture: ComponentFixture<HistoryComponent>;
  let component: HistoryComponent;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HistoryComponent, HttpClientTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(HistoryComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('carga el historial al iniciar', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/generations`);
    req.flush([{ id: 'gen-1', profileSlug: 'periodistico', createdAt: '2026-07-15T00:00:00Z', rating: null }]);

    expect(component.items().length).toBe(1);
    expect(component.loading()).toBeFalse();
  });

  it('open() carga el detalle y prellena el rating', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/generations`).flush([]);

    component.open('gen-1');

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/generations/gen-1`);
    req.flush({ id: 'gen-1', profileSlug: 'periodistico', rating: 4, resultMarkdown: '# Título' } as any);

    expect(component.selected()?.id).toBe('gen-1');
    expect(component.ratingValue).toBe(4);
  });

  it('rate() hace PATCH y actualiza el detalle seleccionado', () => {
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/generations`).flush([]);

    component.open('gen-1');
    httpMock.expectOne(`${environment.apiBaseUrl}/api/generations/gen-1`).flush({ id: 'gen-1', profileSlug: 'periodistico', rating: null } as any);

    component.ratingValue = 5;
    component.rate();

    const patchReq = httpMock.expectOne(`${environment.apiBaseUrl}/api/generations/gen-1/rating`);
    expect(patchReq.request.method).toBe('PATCH');
    patchReq.flush(null);

    // rate() dispara reload()
    httpMock.expectOne(`${environment.apiBaseUrl}/api/generations`).flush([]);

    expect(component.selected()?.rating).toBe(5);
  });
});
