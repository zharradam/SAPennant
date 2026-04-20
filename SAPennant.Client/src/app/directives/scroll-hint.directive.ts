import { Directive, ElementRef, OnInit, OnDestroy, Renderer2 } from '@angular/core';

@Directive({
  selector: '[scrollHint]',
  standalone: false
})
export class ScrollHintDirective implements OnInit, OnDestroy {
  private hintRow!: HTMLTableRowElement;
  private leftArrow!: HTMLSpanElement;
  private rightArrow!: HTMLSpanElement;
  private scrollListener!: () => void;
  private resizeObserver!: ResizeObserver;

  constructor(private el: ElementRef<HTMLElement>, private renderer: Renderer2) {}

  ngOnInit(): void {
    const wrap = this.el.nativeElement;

    // Build the hint row — we'll inject it into the thead after init
    setTimeout(() => {
      const thead = wrap.querySelector('thead');
      if (!thead) return;

      this.hintRow = this.renderer.createElement('tr');
      this.renderer.addClass(this.hintRow, 'scroll-hint-row');

      const th = this.renderer.createElement('th');
      // colspan = number of th elements in first header row
      const colCount = thead.querySelectorAll('tr:first-child th').length;
      this.renderer.setAttribute(th, 'colspan', String(colCount));

      const inner = this.renderer.createElement('div');
      this.renderer.addClass(inner, 'scroll-hint-inner');

      this.leftArrow = this.renderer.createElement('span');
      this.leftArrow.innerHTML = `<svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round"><path d="M10 3l-5 5 5 5"/></svg>`;
      this.renderer.addClass(this.leftArrow, 'scroll-arrow');
      this.renderer.addClass(this.leftArrow, 'scroll-arrow-left');

      const label = this.renderer.createElement('span');
      label.textContent = 'Scroll for more';

      this.rightArrow = this.renderer.createElement('span');
      this.rightArrow.innerHTML = `<svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round"><path d="M6 3l5 5-5 5"/></svg>`;
      this.renderer.addClass(this.rightArrow, 'scroll-arrow');
      this.renderer.addClass(this.rightArrow, 'scroll-arrow-right');

      inner.appendChild(this.leftArrow);
      inner.appendChild(label);
      inner.appendChild(this.rightArrow);
      th.appendChild(inner);
      this.hintRow.appendChild(th);
      thead.appendChild(this.hintRow);

      this.updateArrows();

      // Listen to scroll on the wrap
      this.scrollListener = this.renderer.listen(wrap, 'scroll', () => this.updateArrows());

      // Also update on resize
      this.resizeObserver = new ResizeObserver(() => this.updateArrows());
      this.resizeObserver.observe(wrap);
    }, 550);
  }

  private updateArrows(): void {
    const el = this.el.nativeElement;
    const isScrollable = el.scrollWidth > el.clientWidth + 4;

    // Show/hide the entire hint row
    if (this.hintRow) {
      this.hintRow.style.display = isScrollable ? '' : 'none';
    }

    if (!isScrollable) return;

    const canLeft = el.scrollLeft > 4;
    const canRight = el.scrollLeft + el.clientWidth < el.scrollWidth - 4;

    this.leftArrow.style.visibility = canLeft ? 'visible' : 'hidden';
    this.rightArrow.style.visibility = canRight ? 'visible' : 'hidden';
  }

  ngOnDestroy(): void {
    if (this.scrollListener) this.scrollListener();
    if (this.resizeObserver) this.resizeObserver.disconnect();
  }
}