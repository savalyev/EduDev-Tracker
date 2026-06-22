/* ===== EduDev Tracker — презентация: навигация, иконки, лайтбокс ===== */
(function() {
    "use strict";

    /* ---------- Иконки инструментов (inline-SVG, полностью оффлайн) ---------- */
    const SVG = {
        code: '<svg viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 7 4 12 9 17"/><polyline points="15 7 20 12 15 17"/></svg>',
        db: '<svg viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2.2"><ellipse cx="12" cy="5" rx="8" ry="3"/><path d="M4 5v14c0 1.7 3.6 3 8 3s8-1.3 8-3V5"/><path d="M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3"/></svg>',
        lock: '<svg viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2.2"><rect x="4" y="10" width="16" height="11" rx="2.5"/><path d="M8 10V7a4 4 0 0 1 8 0v3"/></svg>',
        bell: '<svg viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 9a6 6 0 0 1 12 0c0 6 2 7 2 7H4s2-1 2-7"/><path d="M10 20a2 2 0 0 0 4 0"/></svg>',
        grid: '<svg viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2.2"><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></svg>',
        link: '<svg viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2.2" stroke-linecap="round"><path d="M9 15l6-6"/><path d="M10.5 6.5l1.5-1.5a4 4 0 0 1 5.6 5.6L16 12"/><path d="M13.5 17.5L12 19a4 4 0 0 1-5.6-5.6L8 11.8"/></svg>',
    };

    const TOOLS = [
        { name: "C#", sub: "Язык бизнес-логики", color: "#68217A", text: "C#" },
        { name: "XAML", sub: "Разметка интерфейса", color: "#0C54C1", svg: SVG.code },
        { name: ".NET MAUI", sub: "Кроссплатформенный UI · net10", color: "#512BD4", text: "MAUI" },
        { name: "Visual Studio 2026", sub: "Среда разработки", color: "#5C2D91", text: "VS" },
        { name: "SQLite", sub: "Локальная база данных", color: "#003B57", svg: SVG.db },
        { name: "Figma", sub: "Макеты экранов", color: "#A259FF", svg: SVG.grid },
    ];

    function renderTools() {
        const host = document.getElementById("tools");
        if (!host) return;
        host.innerHTML = TOOLS.map(function(t) {
            const inner = t.svg ? t.svg : '<span>' + t.text + '</span>';
            const fs = t.text ? 'font-size:clamp(.9rem,1.3vw,1.5rem)' : '';
            return (
                '<div class="tool">' +
                '<div class="tool__badge" style="background:' + t.color + ';' + fs + '">' + inner + '</div>' +
                '<div><div class="tool__name">' + t.name + '</div>' +
                '<div class="tool__sub">' + t.sub + '</div></div>' +
                '</div>'
            );
        }).join("");
    }

    /* ---------- Навигация по слайдам ---------- */
    const deck = document.getElementById("deck");
    const slides = Array.from(document.querySelectorAll("[data-slide]"));
    const total = slides.length;
    const bar = document.getElementById("progressBar");
    const dotsWrap = document.getElementById("dots");
    const curNumEl = document.getElementById("curNum");
    const prevBtn = document.getElementById("prevBtn");
    const nextBtn = document.getElementById("nextBtn");
    let index = 0;

    document.getElementById("totalNum").textContent = String(total);

    // точки
    slides.forEach(function(_, i) {
        const d = document.createElement("button");
        d.className = "dot";
        d.setAttribute("aria-label", "Слайд " + (i + 1));
        d.addEventListener("click", function() { go(i); });
        dotsWrap.appendChild(d);
    });
    const dots = Array.from(dotsWrap.children);

    function render() {
        deck.style.transform = "translateX(" + (-index * 100) + "vw)";
        bar.style.width = ((index + 1) / total * 100) + "%";
        curNumEl.textContent = String(index + 1);
        dots.forEach(function(d, i) { d.classList.toggle("is-active", i === index); });
        prevBtn.disabled = index === 0;
        nextBtn.disabled = index === total - 1;
        if (location.hash !== "#" + (index + 1)) {
            history.replaceState(null, "", "#" + (index + 1));
        }
    }

    function go(i) {
        index = Math.max(0, Math.min(total - 1, i));
        render();
    }

    function next() { if (index < total - 1) go(index + 1); }

    function prev() { if (index > 0) go(index - 1); }

    nextBtn.addEventListener("click", next);
    prevBtn.addEventListener("click", prev);

    // клавиатура
    document.addEventListener("keydown", function(e) {
        if (!lb.hidden) return; // в лайтбоксе свои клавиши
        switch (e.key) {
            case "ArrowRight":
            case "PageDown":
            case " ":
                e.preventDefault();
                next();
                break;
            case "ArrowLeft":
            case "PageUp":
                prev();
                break;
            case "Home":
                go(0);
                break;
            case "End":
                go(total - 1);
                break;
            case "f":
            case "F":
            case "а":
            case "А":
                toggleFullscreen();
                break;
        }
    });

    function toggleFullscreen() {
        if (!document.fullscreenElement) {
            (document.documentElement.requestFullscreen || function() {}).call(document.documentElement);
        } else if (document.exitFullscreen) {
            document.exitFullscreen();
        }
    }

    // открытие на нужном слайде по хэшу (#3)
    const fromHash = parseInt((location.hash || "").slice(1), 10);
    if (fromHash >= 1 && fromHash <= total) index = fromHash - 1;

    /* ---------- Лайтбокс: зум + пан диаграмм ---------- */
    const lb = document.getElementById("lightbox");
    const lbStage = document.getElementById("lbStage");
    const lbImg = document.getElementById("lbImg");
    let scale = 1,
        fit = 1,
        tx = 0,
        ty = 0;
    let dragging = false,
        dragX = 0,
        dragY = 0;

    function applyTransform() {
        lbImg.style.transform =
            "translate(-50%, -50%) translate(" + tx + "px, " + ty + "px) scale(" + scale + ")";
    }

    function fitToStage() {
        const sw = lbStage.clientWidth,
            sh = lbStage.clientHeight;
        const iw = lbImg.naturalWidth || lbImg.clientWidth || sw;
        const ih = lbImg.naturalHeight || lbImg.clientHeight || sh;
        lbImg.style.width = iw + "px";
        lbImg.style.height = ih + "px";
        fit = Math.min(sw / iw, sh / ih) * 0.94;
        if (!isFinite(fit) || fit <= 0) fit = 1;
        scale = fit;
        tx = 0;
        ty = 0;
        applyTransform();
    }

    function openLightbox(src, alt) {
        lbImg.src = src;
        lbImg.alt = alt || "";
        lb.hidden = false;
        if (lbImg.complete && lbImg.naturalWidth) fitToStage();
        else lbImg.onload = fitToStage;
    }

    function closeLightbox() {
        lb.hidden = true;
        lbImg.src = "";
    }

    function zoomAt(factor, cx, cy) {
        const min = fit * 0.6,
            max = fit * 12;
        const newScale = Math.max(min, Math.min(max, scale * factor));
        const k = newScale / scale;
        // удерживаем точку под курсором (cx,cy — относительно центра сцены)
        tx = cx - (cx - tx) * k;
        ty = cy - (cy - ty) * k;
        scale = newScale;
        applyTransform();
    }

    // клик по диаграмме на слайде → открыть
    document.querySelectorAll(".figure__frame").forEach(function(frame) {
        frame.addEventListener("click", function() {
            const img = frame.querySelector("img.diagram");
            if (img) openLightbox(img.getAttribute("src"), img.alt);
        });
    });

    // панель управления
    lb.querySelector(".lightbox__bar").addEventListener("click", function(e) {
        const btn = e.target.closest("button");
        if (!btn) return;
        const stageC = { x: 0, y: 0 }; // центр сцены
        switch (btn.dataset.zoom) {
            case "in":
                zoomAt(1.25, stageC.x, stageC.y);
                break;
            case "out":
                zoomAt(0.8, stageC.x, stageC.y);
                break;
            case "reset":
                fitToStage();
                break;
            case "close":
                closeLightbox();
                break;
        }
    });

    // зум колесом к курсору
    lbStage.addEventListener("wheel", function(e) {
        e.preventDefault();
        const rect = lbStage.getBoundingClientRect();
        const cx = e.clientX - rect.left - rect.width / 2;
        const cy = e.clientY - rect.top - rect.height / 2;
        zoomAt(e.deltaY < 0 ? 1.12 : 0.89, cx, cy);
    }, { passive: false });

    // пан перетаскиванием (мышь + тач через pointer events)
    lbStage.addEventListener("pointerdown", function(e) {
        dragging = true;
        dragX = e.clientX - tx;
        dragY = e.clientY - ty;
        lbStage.classList.add("is-grabbing");
        lbStage.setPointerCapture(e.pointerId);
    });
    lbStage.addEventListener("pointermove", function(e) {
        if (!dragging) return;
        tx = e.clientX - dragX;
        ty = e.clientY - dragY;
        applyTransform();
    });

    function endDrag() {
        dragging = false;
        lbStage.classList.remove("is-grabbing");
    }
    lbStage.addEventListener("pointerup", endDrag);
    lbStage.addEventListener("pointercancel", endDrag);

    // клавиши внутри лайтбокса
    document.addEventListener("keydown", function(e) {
        if (lb.hidden) return;
        if (e.key === "Escape") closeLightbox();
        else if (e.key === "+" || e.key === "=") zoomAt(1.25, 0, 0);
        else if (e.key === "-") zoomAt(0.8, 0, 0);
        else if (e.key === "0") fitToStage();
    });

    // двойной клик в лайтбоксе — переключить зум
    lbStage.addEventListener("dblclick", function(e) {
        const rect = lbStage.getBoundingClientRect();
        const cx = e.clientX - rect.left - rect.width / 2;
        const cy = e.clientY - rect.top - rect.height / 2;
        if (scale > fit * 1.2) fitToStage();
        else zoomAt(2.2, cx, cy);
    });

    window.addEventListener("resize", function() { if (!lb.hidden) fitToStage(); });

    /* ---------- Старт ---------- */
    renderTools();
    render();
})();