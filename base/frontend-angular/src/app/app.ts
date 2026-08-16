import { Component } from '@angular/core';

import { PlanosLista } from './planos/planos-lista';
import { BeneficiariosComponent } from './beneficiarios/beneficiarios-component/beneficiarios-component';

@Component({
  selector: 'app-root',
  imports: [PlanosLista, BeneficiariosComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}
