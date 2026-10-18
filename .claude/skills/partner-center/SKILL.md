---
name: partner-center
description: Prepara un nuovo invio di SmartClip al Microsoft Store (Partner Center) e raccoglie in store\upload tutto ciò che va caricato - pacchetto MSIX, CSV della scheda in quattro lingue e immagini. Da usare quando l'utente chiede di preparare Partner Center, l'invio allo Store, l'aggiornamento della scheda o "fai tutto per Partner Center".
---

# Preparazione per Partner Center

Scopo: alla fine `store\upload\` contiene **tutto e solo** ciò che l'utente carica in Partner Center:

```
store\upload\
  SmartClip-<versione>.0.msix      ← Packages: si trascina nella pagina dei pacchetti
  SmartClip-listing\               ← Store listings → Import listings → Import folder
    listing.csv                      (testi delle quattro lingue)
    images\<en|es|fr|it>\*.png       (schermate) e images\tile-300.png
```

`store\upload\` non sta in git (`.gitignore`). Tutto il resto (testi, immagini sorgente, script) sì. Rispondere
all'utente in italiano.

## 1. Controlli di partenza

- Versione: `AssemblyVersion` in `src\Program.cs`. La quarta cifra deve essere 0 (lo Store la vuole così). Deve
  essere più alta dell'ultima inviata: se l'utente non l'ha già cambiata, chiedere quale numero vuole prima di
  toccarla (è anche ciò che fa partire la release su GitHub).
- `Get-Process SmartClip`: deve essere chiuso. Altrimenti la compilazione fallisce e le immagini dello Store
  mostrano le scorciatoie occupate. Se è aperto, chiedere all'utente di chiuderlo (anche dal tray, «Esci»).
- `git status`: capire che cosa è cambiato dall'ultima versione inviata (`git log` dall'ultimo tag `v*`,
  `gh release list`), per le novità della versione.

## 2. Compilare e controllare

- `build.cmd < NUL` (da `cmd /c`).
- I controlli senza finestre: compilare `tests.exe` **con l'icona** (`/resource:SmartClip.ico,SmartClip.ico`,
  stessi `/r:` di `tests\test.cmd`), poi `tests\tests.exe texts`. Deve finire con codice 0: le righe «same as
  English» sono normali (Error, Options, Ok, No).
- `tests\tests.exe type` ruba il primo piano per qualche secondo: lanciarlo solo dopo averlo detto all'utente.

## 3. La scheda in `store\README.md`

Per ognuna delle quattro sezioni (`## English (en-US)`, `## Italiano (it-IT)`, `## Español (es-ES)`,
`## Français (fr-FR)`), con lo stesso contenuto tradotto:

- **Novità di questa versione** (il sesto blocco di codice): riscriverlo per la versione nuova, a partire dai
  commit. Massimo 1500 caratteri. Se non si sa se la versione precedente è stata davvero pubblicata sullo Store,
  chiederlo: le sue novità vanno tenute solo se non è mai arrivata.
- **Caratteristiche**: aggiungere o aggiornare le righe delle funzioni nuove. Massimo 20 righe, ognuna massimo
  200 caratteri. Se si supera, accorpare quelle esistenti.
- Descrizione, descrizione breve, termini di ricerca (massimo 7) e didascalie solo se le funzioni nuove le
  rendono false o incomplete.
- L'ordine dei blocchi di codice in ogni sezione non va cambiato: `make-listing.ps1` li legge per posizione
  (nome, descrizione breve, descrizione, caratteristiche, termini, novità).
- Se la guida o le opzioni sono cambiate, controllare che anche `README.md` (inglese) le dica.

## 4. Le immagini

- Schermate dello Store, una lingua alla volta (le finestre compaiono qualche secondo, avvisare l'utente):
  `tests\tests.exe store store\images <en|es|fr|it>`. Guardarne almeno un paio con Read (elenco e opzioni) per
  scoprire testi tagliati.
- Se l'aspetto o le opzioni sono cambiati, anche le schermate del README: `tests\tests.exe real <cartella
  temporanea>\docl en light` e `... docd en dark`. Poi copiare `docl-real-list.png` in `docs\screenshot.png`,
  `-real-options` in `docs\screenshot-options.png`, `-real-edit` in `docs\screenshot-edit.png` e
  `docd-real-list.png` in `docs\screenshot-dark.png`.
- Dopo, controllare con `reg query HKCU\Software\SmartClip /v Language` che le impostazioni dell'utente non siano
  cambiate.

## 5. Il CSV e le immagini da importare

`powershell -ExecutionPolicy Bypass -File store\make-listing.ps1`

Senza argomenti riparte dal `listing.csv` scritto la volta prima. Se l'utente ha esportato un CSV nuovo da
Partner Center (*Store listings → Export listings*), passare quello: contiene i campi aggiunti nel frattempo.
Lo script riscrive `store\upload\SmartClip-listing` e non tocca il pacchetto.

Verifica: rileggere con `Microsoft.VisualBasic.FileIO.TextFieldParser` il CSV di prima (copiato altrove prima di
lanciare lo script) e quello nuovo, e confrontarli cella per cella. Devono cambiare solo i campi che hai
cambiato in `store\README.md`.

## 6. Commit, push e pacchetto

- Commit dei sorgenti, dei testi e delle immagini, con messaggio in inglese come i precedenti e la riga
  `Co-Authored-By`.
- Poi il push, **senza chiedere**: chiedere la skill vale già come consenso (detto dall'utente il 18/10/2026: «se
  ti chiedo la skill partner fai tutto»). Pubblica la release `vX.Y.Z` su GitHub.
- Dopo il push, il flusso *Release* crea il pacchetto (qualche minuto): `gh run list --workflow release.yml
  --limit 1`, poi `gh run watch <id> --exit-status`.
- `powershell -ExecutionPolicy Bypass -File store\get-package.ps1` scarica in `store\upload` il `.msix` della
  versione di `Program.cs` e toglie quelli vecchi. Se la release esisteva già e il pacchetto non c'è,
  `get-package.ps1 -Build` avvia il flusso a mano (da ciò che è su `main`) e aspetta.

## 7. Consegna

Mostrare all'utente il contenuto di `store\upload` e i passi in Partner Center:

1. *Apps and games* → SmartClip → **Start update** (o *Update* sull'invio esistente).
2. **Packages**: caricare `store\upload\SmartClip-<versione>.0.msix`, togliere il pacchetto vecchio se rimane.
3. **Store listings** → *Import listings* → **Import folder** → `store\upload\SmartClip-listing`. Controllare le
   schede delle quattro lingue.
4. Il resto (prezzi, proprietà, classificazione) non cambia: vedi `store\README.md`.
5. **Submit to the Store**.

Dire anche che cosa non è stato verificato (per esempio il test `type` non lanciato).
