
// Função para alternar alto contraste
function toggleAltoContraste() {
    const body = document.body;
    const isAltoContraste = body.classList.contains('alto-contraste');

    if (isAltoContraste) {
        body.classList.remove('alto-contraste');
        localStorage.setItem('altoContraste', 'false');
    } else {
        body.classList.add('alto-contraste');
        localStorage.setItem('altoContraste', 'true');
    }
}

// Aplicar alto contraste ao carregar a página
document.addEventListener('DOMContentLoaded', function () {
    const altoContrasteAtivo = localStorage.getItem('altoContraste') === 'true';
    if (altoContrasteAtivo) {
        document.body.classList.add('alto-contraste');
    }
});

// Menu de Acessibilidade Mobile
document.addEventListener('DOMContentLoaded', function () {
    const toggleBtn = document.getElementById('btnToggleAcessibilidade');
    const menuContent = document.getElementById('menuAcessibilidadeContent');

    if (toggleBtn && menuContent) {
        toggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            const isExpanded = this.getAttribute('aria-expanded') === 'true';
            this.setAttribute('aria-expanded', !isExpanded);
            menuContent.style.display = isExpanded ? 'none' : 'block';
            menuContent.setAttribute('aria-hidden', isExpanded);

            if (!isExpanded) {
                this.classList.add('active');
                menuContent.classList.add('show');
            } else {
                this.classList.remove('active');
                menuContent.classList.remove('show');
            }
        });

        document.addEventListener('click', function (e) {
            if (!toggleBtn.contains(e.target) && !menuContent.contains(e.target)) {
                toggleBtn.setAttribute('aria-expanded', 'false');
                toggleBtn.classList.remove('active');
                menuContent.style.display = 'none';
                menuContent.setAttribute('aria-hidden', 'true');
                menuContent.classList.remove('show');
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && menuContent.style.display === 'block') {
                toggleBtn.setAttribute('aria-expanded', 'false');
                toggleBtn.classList.remove('active');
                menuContent.style.display = 'none';
                menuContent.setAttribute('aria-hidden', 'true');
                menuContent.classList.remove('show');
                toggleBtn.focus();
            }
        });
    }
});

// Aumentar e diminuir letra
let tamanhoBase = 1.25; // tamanho padrão = 1.25rem
const html = document.documentElement;

// Pega todos os botões equivalentes (desktop + mobile)
const btnsAumentar = document.querySelectorAll(".btn-aumentar-fonte");
const btnsDiminuir = document.querySelectorAll(".btn-diminuir-fonte");
const btnsContraste = document.querySelectorAll(".btn-alto-contraste");

// Atualiza tamanho da fonte
function atualizarTamanhoFonte() {
    html.style.fontSize = `${tamanhoBase}rem`;
}

// Aumentar fonte
btnsAumentar.forEach((btn) =>
    btn.addEventListener("click", () => {
        if (tamanhoBase < 1.5) {
            tamanhoBase += 0.05;
            atualizarTamanhoFonte();
        }
    })
);

// Diminuir fonte
btnsDiminuir.forEach((btn) =>
    btn.addEventListener("click", () => {
        if (tamanhoBase > 0.8) {
            tamanhoBase -= 0.05;
            atualizarTamanhoFonte();
        }
    })
);

window.addEventListener('DOMContentLoaded', () => {
    const CONTRASTE_KEY = 'altoContraste';
    const TOGGLE_SELECTOR = '.btn-alto-contraste';
    const rootHtml = document.documentElement; // <html>
    const rootBody = document.body; // <body>

    // Aplica/Remove classe em ambos para evitar conflitos de CSS
    function aplicarContraste(on) {
        rootHtml.classList.toggle('alto-contraste', !!on);
        rootBody.classList.toggle('alto-contraste', !!on);

        // seta aria-pressed em todos os botões (se existirem)
        const botoes = document.querySelectorAll(TOGGLE_SELECTOR);
        botoes.forEach(b => b.setAttribute('aria-pressed', on ? 'true' : 'false'));

        try {
            localStorage.setItem(CONTRASTE_KEY, on ? '1' : '0');
        } catch (err) {
            // ignore se localStorage não estiver disponível
        }
    }

    // lê preferência salva (se houver)
    let pref = false;
    try {
        pref = localStorage.getItem(CONTRASTE_KEY) === '1';
    } catch (err) {
        pref = false;
    }
    aplicarContraste(pref);

    // Delegação de clique — suporta ícone interno (closest)
    document.addEventListener('click', (e) => {
        const btn = e.target.closest(TOGGLE_SELECTOR);
        if (!btn) return;
        e.preventDefault();
        const ativo = rootHtml.classList.contains('alto-contraste') || rootBody.classList.contains('alto-contraste');
        aplicarContraste(!ativo);
    });

    // Suporte teclado (Enter / Space) quando o botão está focado
    document.addEventListener('keydown', (e) => {
        const el = document.activeElement;
        if (!el || !el.matches) return;
        if (!el.matches(TOGGLE_SELECTOR)) return;

        if (e.key === ' ' || e.key === 'Spacebar' || e.key === 'Enter') {
            e.preventDefault();
            const ativo = rootHtml.classList.contains('alto-contraste') || rootBody.classList.contains('alto-contraste');
            aplicarContraste(!ativo);
        }
    });
});


