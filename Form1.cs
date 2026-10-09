using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace OlivettiEmulatore
{
    public partial class Form1 : Form
    {
        // Modalità vista: 0 = Compatta, 1 = Super Compatta (6 righe), 2 = Estesa
        private int statoVista = 0;

        // Registri di Calcolo
        private decimal displayValore = 0;
        private decimal registroPrecedente = 0;
        private string operazioneInAttesa = "";
        private decimal totalizzatore1 = 0;
        private decimal totalizzatore2 = 0;
        private decimal totaleGenerale = 0;
        private int conteggioArticoli = 0;
        private decimal aliquotaIVA = 22.0m;

        // Variabili di Stato Calcoli Speciali
        private decimal baseLordoSpeciale = 0;
        private decimal nettoInversoSpeciale = 0;
        private decimal imponibileScorporo = 0;

        private string bufferInput = "";
        private bool nuovaImmissione = true;

        // Gestione Zoom Vista Compatta (100%, 120%, 150%)
        private int indiceScalaCompatta = 0;
        private readonly float[] fattoriScala = { 1.0f, 1.2f, 1.5f };

        private DateTime ultimoTastoPremutoTempo = DateTime.MinValue;
        private Keys ultimoTastoPremuto = Keys.None;

        // Selettori di Stato
        private bool selettoreDecimaliAddizione = false;
        private bool selettoreACC = true;
        private bool selettoreIC = true;
        private int modalitaArrotondamento = 5;
        private bool sempreInPrimoPiano = false;

        // Controlli Visivi Interfaccia
        private Label lblDisplay = null!;
        private TextBox txtNastro = null!;
        private Panel pnlTastiSpeciali = null!;
        private GroupBox grpSelettori = null!;
        private Button btnGuida = null!;
        private Button btnAvanzaCarta = null!;
        private Button btnSalvaNastro = null!;
        private Button btnPulisciNastro = null!;
        private Button btnToggleCompatto = null!;
        private Button btnResizeCompatto = null!;

        // Controlli Interruttori Mouse
        private CheckBox chkACC = null!;
        private CheckBox chkIC = null!;
        private CheckBox chkTopMost = null!;
        private RadioButton rdoDecStd = null!;
        private RadioButton rdoDecAdd = null!;
        private RadioButton rdoArr0 = null!;
        private RadioButton rdoArr5 = null!;
        private RadioButton rdoArr9 = null!;

        public Form1()
        {
            BuildUI();
            this.KeyPreview = true;
            this.KeyPress += Form1_KeyPress;
            ApplicaStatoVista();
        }

        private void BuildUI()
        {
            this.Text = "Pascalina++ minimal";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OlivettiLogos.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }
            else
            {
                try
                {
                    this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
                catch { }
            }

            // Display LCD
            lblDisplay = new Label
            {
                Text = "0",
                Font = new Font("Consolas", 15, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = Color.Black,
                ForeColor = Color.Lime
            };

            // Switch Vista F6
            btnToggleCompatto = new Button
            {
                Text = "↔ Vista Super (F6)",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.Gainsboro,
                FlatStyle = FlatStyle.System,
                TabStop = false
            };
            btnToggleCompatto.Click += (s, e) => CiclaStatoVista();

            // Zoom Vista Compatta
            btnResizeCompatto = new Button
            {
                Text = "🔍 100% (Z)",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.System,
                TabStop = false
            };
            btnResizeCompatto.Click += (s, e) => CizlaDimensioniCompatte();

            // Pannello Selettori
            grpSelettori = new GroupBox
            {
                Text = "Selettori e Modalità",
                Location = new Point(10, 90),
                Size = new Size(500, 130),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Visible = false
            };

            btnGuida = new Button
            {
                Text = "? Guida (F1)",
                Location = new Point(390, 15),
                Size = new Size(100, 23),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.LightSteelBlue,
                FlatStyle = FlatStyle.System,
                TabStop = false
            };
            btnGuida.Click += (s, e) => MostraGuidaTasti();

            btnAvanzaCarta = new Button
            {
                Text = "↑ Avanza",
                Location = new Point(295, 15),
                Size = new Size(90, 23),
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                FlatStyle = FlatStyle.System,
                TabStop = false
            };
            btnAvanzaCarta.Click += (s, e) => AvanzaNastroCarta();

            chkACC = new CheckBox
            {
                Text = "ACC / GT (F3)",
                Checked = selettoreACC,
                Location = new Point(15, 20),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 8),
                TabStop = false
            };
            chkACC.CheckedChanged += (s, e) => { selettoreACC = chkACC.Checked; };

            chkIC = new CheckBox
            {
                Text = "IC Conteggio (F4)",
                Checked = selettoreIC,
                Location = new Point(120, 20),
                Size = new Size(110, 20),
                Font = new Font("Segoe UI", 8),
                TabStop = false
            };
            chkIC.CheckedChanged += (s, e) => { selettoreIC = chkIC.Checked; };

            Label lblDec = new Label { Text = "Decimali (F2):", Location = new Point(15, 48), Size = new Size(80, 15), Font = new Font("Segoe UI", 8, FontStyle.Regular) };
            rdoDecStd = new RadioButton { Text = "Standard", Checked = true, Location = new Point(100, 46), Size = new Size(75, 20), Font = new Font("Segoe UI", 8, FontStyle.Regular), TabStop = false };
            rdoDecAdd = new RadioButton { Text = "Modalità '+'", Location = new Point(180, 46), Size = new Size(95, 20), Font = new Font("Segoe UI", 8, FontStyle.Regular), TabStop = false };
            rdoDecStd.CheckedChanged += (s, e) => { if (rdoDecStd.Checked) selettoreDecimaliAddizione = false; };
            rdoDecAdd.CheckedChanged += (s, e) => { if (rdoDecAdd.Checked) selettoreDecimaliAddizione = true; };

            Label lblArr = new Label { Text = "Arrotondamento (F5):", Location = new Point(15, 75), Size = new Size(120, 15), Font = new Font("Segoe UI", 8, FontStyle.Regular) };
            rdoArr0 = new RadioButton { Text = "Difetto (0)", Location = new Point(140, 73), Size = new Size(80, 20), Font = new Font("Segoe UI", 8, FontStyle.Regular), TabStop = false };
            rdoArr5 = new RadioButton { Text = "Comm. (5)", Checked = true, Location = new Point(225, 73), Size = new Size(80, 20), Font = new Font("Segoe UI", 8, FontStyle.Regular), TabStop = false };
            rdoArr9 = new RadioButton { Text = "Eccesso (9)", Location = new Point(310, 73), Size = new Size(85, 20), Font = new Font("Segoe UI", 8, FontStyle.Regular), TabStop = false };
            rdoArr0.CheckedChanged += (s, e) => { if (rdoArr0.Checked) modalitaArrotondamento = 0; };
            rdoArr5.CheckedChanged += (s, e) => { if (rdoArr5.Checked) modalitaArrotondamento = 5; };
            rdoArr9.CheckedChanged += (s, e) => { if (rdoArr9.Checked) modalitaArrotondamento = 9; };

            chkTopMost = new CheckBox
            {
                Text = "📌 Sempre in Primo Piano (F7)",
                Checked = sempreInPrimoPiano,
                Location = new Point(15, 100),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                TabStop = false
            };
            chkTopMost.CheckedChanged += (s, e) => ToggleSempreInPrimoPiano(chkTopMost.Checked);

            grpSelettori.Controls.Add(btnGuida);
            grpSelettori.Controls.Add(btnAvanzaCarta);
            grpSelettori.Controls.Add(chkACC);
            grpSelettori.Controls.Add(chkIC);
            grpSelettori.Controls.Add(lblDec);
            grpSelettori.Controls.Add(rdoDecStd);
            grpSelettori.Controls.Add(rdoDecAdd);
            grpSelettori.Controls.Add(lblArr);
            grpSelettori.Controls.Add(rdoArr0);
            grpSelettori.Controls.Add(rdoArr5);
            grpSelettori.Controls.Add(rdoArr9);
            grpSelettori.Controls.Add(chkTopMost);

            // Pannello Verticale Tasti Speciali
            pnlTastiSpeciali = new Panel
            {
                BackColor = Color.FromArgb(235, 237, 240)
            };

            // Nastro Stampa
            txtNastro = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FloralWhite,
                TabStop = false
            };

            // Pulsanti Gestione Nastro
            btnSalvaNastro = new Button
            {
                Text = "💾 Salva (Ctrl+S)",
                Location = new Point(10, 665),
                Size = new Size(245, 30),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.Honeydew,
                FlatStyle = FlatStyle.System,
                TabStop = false,
                Visible = false
            };
            btnSalvaNastro.Click += (s, e) => SalvaNastroSuFile();

            btnPulisciNastro = new Button
            {
                Text = "🧹 Pulisci (Ctrl+Del)",
                Location = new Point(265, 665),
                Size = new Size(245, 30),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.MistyRose,
                FlatStyle = FlatStyle.System,
                TabStop = false,
                Visible = false
            };
            btnPulisciNastro.Click += (s, e) => PulisciNastro();

            this.Controls.Add(lblDisplay);
            this.Controls.Add(btnToggleCompatto);
            this.Controls.Add(btnResizeCompatto);
            this.Controls.Add(grpSelettori);
            this.Controls.Add(pnlTastiSpeciali);
            this.Controls.Add(txtNastro);
            this.Controls.Add(btnSalvaNastro);
            this.Controls.Add(btnPulisciNastro);
        }

        private void PopolaPannelloTastiSpeciali(float scala)
        {
            pnlTastiSpeciali.Controls.Clear();

            var tasti = new (string Tasto, string Desc, Action Azione)[]
            {
                ("NOT", "Nota\n^⇧N", () => InserisciNotaManuale()),
                ("#/D", "Data\nOra/#", () => StampaRiferimentoDataOra()),
                ("K", "Sc.\nX+10", () => GestisciCalcoloScontoX10()),
                ("⇧ K", "Inv.\nLordo", () => GestisciCalcoloLordoInverso()),
                ("⇧ I", "10/22\nIVA", () => GestisciScorporoDueAliquote()),
                ("T", "+TAX\n22%", () => CalcolaScatoleIVA(true)),
                ("⇧ T", "-TAX\n22%", () => CalcolaScatoleIVA(false)),
                ("D", "Δ%\nDelta", () => CalcolaDeltaPercentuale()),
                ("R", "√\nRadice", () => CalcolaRadiceQuadrata())
            };

            int numeroTasti = tasti.Length;
            int boxWidth = pnlTastiSpeciali.Width - 6;

            int spacing = 3;
            int altezzaDisponibile = pnlTastiSpeciali.Height - 6 - (spacing * (numeroTasti - 1));
            int boxHeight = altezzaDisponibile / numeroTasti;

            if (boxHeight < 24) boxHeight = 24;

            int y = 3;
            float fontDim = Math.Max(5.8f, 6.5f * (boxHeight / 44.0f));

            foreach (var t in tasti)
            {
                Button btnBadge = new Button
                {
                    Location = new Point(3, y),
                    Size = new Size(boxWidth, boxHeight),
                    Text = $"{t.Tasto}\n{t.Desc}",
                    Font = new Font("Segoe UI", fontDim, FontStyle.Bold),
                    BackColor = Color.White,
                    ForeColor = Color.DarkSlateGray,
                    FlatStyle = FlatStyle.Flat,
                    TabStop = false,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btnBadge.FlatAppearance.BorderColor = Color.LightGray;
                btnBadge.Click += (s, e) => t.Azione();
                pnlTastiSpeciali.Controls.Add(btnBadge);

                y += boxHeight + spacing;
            }
        }

        private void CiclaStatoVista()
        {
            statoVista = (statoVista + 1) % 3;
            ApplicaStatoVista();
        }

        private void ApplicaStatoVista()
        {
            switch (statoVista)
            {
                case 0: // Vista Compatta standard
                    grpSelettori.Visible = false;
                    btnSalvaNastro.Visible = false;
                    btnPulisciNastro.Visible = false;
                    btnResizeCompatto.Visible = true;
                    pnlTastiSpeciali.Visible = true;
                    ApplicaLayoutCompatto();
                    break;

                case 1: // Vista Super Compatta (6 righe, no pulsanti laterali, no zoom)
                    grpSelettori.Visible = false;
                    btnSalvaNastro.Visible = false;
                    btnPulisciNastro.Visible = false;
                    btnResizeCompatto.Visible = false;
                    pnlTastiSpeciali.Visible = false;
                    ApplicaLayoutSuperCompatto();
                    break;

                case 2: // Vista Estesa completa
                    btnResizeCompatto.Visible = false;
                    pnlTastiSpeciali.Visible = true;
                    grpSelettori.Visible = true;
                    btnSalvaNastro.Visible = true;
                    btnPulisciNastro.Visible = true;

                    this.ClientSize = new Size(520, 710);
                    lblDisplay.Location = new Point(10, 10);
                    lblDisplay.Size = new Size(500, 45);
                    lblDisplay.Font = new Font("Consolas", 20, FontStyle.Bold);

                    btnToggleCompatto.Location = new Point(10, 60);
                    btnToggleCompatto.Size = new Size(500, 25);
                    btnToggleCompatto.Text = "↔ Vista Compatta (F6)";

                    pnlTastiSpeciali.Location = new Point(10, 230);
                    pnlTastiSpeciali.Size = new Size(58, 425);
                    PopolaPannelloTastiSpeciali(1.0f);

                    txtNastro.Location = new Point(72, 230);
                    txtNastro.Size = new Size(438, 425);
                    txtNastro.Font = new Font("Consolas", 10);
                    break;
            }
        }

        private void ApplicaLayoutCompatto()
        {
            float scala = fattoriScala[indiceScalaCompatta];

            btnResizeCompatto.Text = indiceScalaCompatta switch
            {
                1 => "🔍 +20% (Z)",
                2 => "🔍 +50% (Z)",
                _ => "🔍 100% (Z)"
            };

            int baseWidth = (int)(320 * scala);
            int baseHeight = (int)(540 * scala);

            this.ClientSize = new Size(baseWidth, baseHeight);

            int margin = 10;
            int innerWidth = baseWidth - (margin * 2);

            lblDisplay.Location = new Point(margin, margin);
            lblDisplay.Size = new Size(innerWidth, (int)(38 * scala));
            lblDisplay.Font = new Font("Consolas", 14 * scala, FontStyle.Bold);

            int btnToggleWidth = (int)(innerWidth * 0.62f);
            int btnResizeWidth = innerWidth - btnToggleWidth - 5;

            btnToggleCompatto.Location = new Point(margin, lblDisplay.Bottom + 5);
            btnToggleCompatto.Size = new Size(btnToggleWidth, 24);
            btnToggleCompatto.Text = "↔ Vista Super (F6)";

            btnResizeCompatto.Location = new Point(btnToggleCompatto.Right + 5, lblDisplay.Bottom + 5);
            btnResizeCompatto.Size = new Size(btnResizeWidth, 24);

            int pnlWidth = (int)(52 * scala);
            int topOffset = btnToggleCompatto.Bottom + 5;
            int nastroHeight = baseHeight - topOffset - margin;

            pnlTastiSpeciali.Location = new Point(margin, topOffset);
            pnlTastiSpeciali.Size = new Size(pnlWidth, nastroHeight);
            PopolaPannelloTastiSpeciali(scala);

            txtNastro.Location = new Point(pnlTastiSpeciali.Right + 4, topOffset);
            txtNastro.Size = new Size(innerWidth - pnlWidth - 4, nastroHeight);
            txtNastro.Font = new Font("Consolas", 9.5f * scala);
        }

        private void ApplicaLayoutSuperCompatto()
        {
            int margin = 8;
            int width = 275;

            // Altezza calcolata per mostrare esattamente 6 righe di testo in Consolas 10pt
            int rigaAltezza = 17;
            int nastroHeight = (rigaAltezza * 6) + 6;

            int lblHeight = 34;
            int btnHeight = 23;
            int totalHeight = margin + lblHeight + 5 + btnHeight + 5 + nastroHeight + margin;

            this.ClientSize = new Size(width, totalHeight);

            int innerWidth = width - (margin * 2);

            lblDisplay.Location = new Point(margin, margin);
            lblDisplay.Size = new Size(innerWidth, lblHeight);
            lblDisplay.Font = new Font("Consolas", 13.5f, FontStyle.Bold);

            btnToggleCompatto.Location = new Point(margin, lblDisplay.Bottom + 5);
            btnToggleCompatto.Size = new Size(innerWidth, btnHeight);
            btnToggleCompatto.Text = "↔ Vista Estesa (F6)";

            txtNastro.Location = new Point(margin, btnToggleCompatto.Bottom + 5);
            txtNastro.Size = new Size(innerWidth, nastroHeight);
            txtNastro.Font = new Font("Consolas", 9.5f);
            txtNastro.ScrollBars = ScrollBars.Vertical;
        }

        private void CizlaDimensioniCompatte()
        {
            indiceScalaCompatta = (indiceScalaCompatta + 1) % 3;
            ApplicaLayoutCompatto();
        }

        private void InserisciNotaManuale()
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 330;
                prompt.Height = 135;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Inserisci Nota (Max 24 caratteri)";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.ShowInTaskbar = false;

                prompt.TopMost = this.TopMost;
                prompt.Owner = this;

                Label textLabel = new Label()
                {
                    Left = 15,
                    Top = 10,
                    Text = "Testo nota / descrizione:",
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9f)
                };

                TextBox textBox = new TextBox()
                {
                    Left = 15,
                    Top = 32,
                    Width = 285,
                    MaxLength = 24,
                    Font = new Font("Consolas", 10.5f)
                };

                Button btnOk = new Button()
                {
                    Text = "Stampa",
                    Left = 215,
                    Width = 85,
                    Top = 64,
                    DialogResult = DialogResult.OK,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
                };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(btnOk);
                prompt.AcceptButton = btnOk;

                if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    txtNastro.AppendText($"  {textBox.Text.Trim(),-20} *" + Environment.NewLine);
                    txtNastro.SelectionStart = txtNastro.TextLength;
                    txtNastro.ScrollToCaret();
                }
            }
        }

        private void StampaRiferimentoDataOra()
        {
            if (string.IsNullOrWhiteSpace(bufferInput) || nuovaImmissione)
            {
                string dataOra = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                txtNastro.AppendText($"  {dataOra,18} #D" + Environment.NewLine);
            }
            else
            {
                txtNastro.AppendText($"  {bufferInput,18} #" + Environment.NewLine);
            }

            txtNastro.SelectionStart = txtNastro.TextLength;
            txtNastro.ScrollToCaret();
            ResetPerNuovaOperazione();
        }

        private void ToggleSempreInPrimoPiano(bool stato)
        {
            sempreInPrimoPiano = stato;
            this.TopMost = sempreInPrimoPiano;
            chkTopMost.Checked = sempreInPrimoPiano;
            AggiornaStileDisplay();
        }

        private void AggiornaStileDisplay()
        {
            if (sempreInPrimoPiano)
            {
                lblDisplay.BackColor = Color.FromArgb(10, 25, 40);
                lblDisplay.ForeColor = Color.Cyan;
            }
            else
            {
                lblDisplay.BackColor = Color.Black;
                lblDisplay.ForeColor = Color.Lime;
            }
        }

        private void Form1_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '#')
            {
                StampaRiferimentoDataOra();
                e.Handled = true;
            }
        }

        private bool MostraAiutoTastoRapido(Keys keyData)
        {
            if ((keyData & Keys.Alt) != Keys.Alt || (keyData & Keys.Control) == Keys.Control)
            {
                return false;
            }

            Keys baseKey = keyData & Keys.KeyCode;
            string titolo = $"Guida Tasto: {baseKey}";
            string? testo = null;

            switch (baseKey)
            {
                case Keys.N:
                    testo = "TASTO N / # / CTRL+SHIFT+N:\n\n" +
                            "• N oppure #:\n" +
                            "  - A vuoto: stampa data e ora correnti (#D)\n" +
                            "  - Con cifre: stampa codice/riferimento non additivo (#)\n\n" +
                            "• CTRL + SHIFT + N:\n" +
                            "  - Apre la finestra per inserire una riga di testo/nota manuale (*)";
                    break;

                case Keys.K:
                    testo = "TASTO K / SHIFT + K (Sconti X+10):\n\n" +
                            "• K (Sconto X):\n" +
                            "  1° tocco: memorizza Lordo/Base (BAS)\n" +
                            "  2° tocco: digita Netto (NET) -> calcola % sconto X (%X10)\n\n" +
                            "• SHIFT + K (Lordo Inverso):\n" +
                            "  1° tocco: memorizza Netto (NET)\n" +
                            "  2° tocco: digita % sconto Y (%SC) -> calcola Lordo originario (LOR)";
                    break;

                case Keys.I:
                    testo = "TASTO SHIFT + I (Scorporo Due Aliquote 22% e 10%):\n\n" +
                            "• 1° tocco: memorizza Imponibile Totale (IMP)\n" +
                            "• 2° tocco: digita Totale Lordo Fattura (LOR) -> scorpora e stampa Imponibile 22% (I22) e 10% (I10)";
                    break;

                case Keys.T:
                    testo = "TASTO T / SHIFT + T (IVA):\n\n" +
                            "• T: Calcola e aggiunge IVA al 22% (+TAX)\n" +
                            "• SHIFT + T: Scorpora/scomputa IVA al 22% (-TAX)";
                    break;

                case Keys.P:
                    testo = "TASTO P (Percentuale Olivetti):\n\n" +
                            "• 1. Inserisci Base e premi * (oppure chiudi un totale con Invio/T1)\n" +
                            "• 2. Digita la % e premi P (%)\n" +
                            "• 3. Premi + per maggiorare (+%) o - per scontare (-%)\n" +
                            "• 4. Per sconti successivi: digita nuova % -> P (%) -> -";
                    break;

                case Keys.D:
                    testo = "TASTO D (Delta Percentuale Δ%):\n\n" +
                            "• 1. Digita il valore iniziale e premi D\n" +
                            "• 2. Digita il valore finale e premi D -> calcola differenza e variazione %";
                    break;

                case Keys.R:
                    testo = "TASTO R (Radice Quadrata √):\n\n" +
                            "• Digita un valore e premi R per calcolarne la radice quadrata";
                    break;

                case Keys.G:
                    testo = "TASTO G (Grand Total / GT):\n\n" +
                            "• Stampa il Totale Generale accumulato da tutte le chiusure T1 e lo azzera";
                    break;

                case Keys.Add:
                case Keys.Oemplus:
                    testo = "TASTO + (Addizione):\n\n" +
                            "• +: Aggiunge al Totalizzatore 1 (+1)\n" +
                            "• SHIFT + +: Aggiunge al Totalizzatore 2 (+2)";
                    break;

                case Keys.Subtract:
                case Keys.OemMinus:
                    testo = "TASTO - (Sottrazione):\n\n" +
                            "• -: Sottrae dal Totalizzatore 1 (-1)\n" +
                            "• SHIFT + -: Sottrae dal Totalizzatore 2 (-2)";
                    break;

                case Keys.Space:
                    testo = "BARRA SPAZIATRICE (Subtotale):\n\n" +
                            "• Spazio: Stampa il Subtotale 1 (S1) senza azzerarlo\n" +
                            "• SHIFT + Spazio: Stampa il Subtotale 2 (S2)";
                    break;

                case Keys.Return:
                    testo = "INVIO (Totale / Esegui):\n\n" +
                    "• Invio: Calcola moltiplicazioni/divisioni o chiude il Totale 1 (T1) azzerandolo\n" +
                    "• SHIFT + Invio: Chiude il Totale 2 (T2) azzerandolo";
                    break;

                case Keys.Multiply:
                case Keys.X:
                    testo = "TASTO * / X (Moltiplicazione):\n\n" +
                            "• Imposta il moltiplicando per il calcolo contabile o per la base percentuale";
                    break;

                case Keys.Divide:
                case Keys.OemQuestion:
                    testo = "TASTO / (Divisione):\n\n" +
                            "• Imposta il dividendo per la divisione contabile";
                    break;

                case Keys.F2:
                    testo = "TASTO F2 (Decimali):\n\n" +
                            "• Alterna modalità standard e modalità '+' (centesimi automatici senza virgola)";
                    break;

                case Keys.F3:
                    testo = "TASTO F3 (ACC / GT):\n\n" +
                            "• Attiva o disattiva l'accumulo nel Gran Totale (GT) a ogni chiusura T1";
                    break;

                case Keys.F4:
                    testo = "TASTO F4 (IC Conteggio):\n\n" +
                            "• Attiva o disattiva il conteggio automatico degli articoli (n#)";
                    break;

                case Keys.F5:
                    testo = "TASTO F5 (Arrotondamento):\n\n" +
                            "• Cicla fra 0 (Difetto/Troncamento), 5 (Commerciale 5/4) e 9 (Eccesso)";
                    break;

                case Keys.F6:
                    testo = "TASTO F6 (Vista):\n\n" +
                            "• Cicla tra Vista Compatta, Super Compatta (6 righe) ed Estesa";
                    break;

                case Keys.F7:
                    testo = "TASTO F7 (Sempre in Primo Piano):\n\n" +
                            "• Mantiene la calcolatrice sempre in sovrimpressione (display azzurro)";
                    break;

                case Keys.Z:
                    testo = "TASTO Z (Zoom Compatto):\n\n" +
                            "• Cicla l'ingrandimento della vista compatta tra 100%, 120% e 150%";
                    break;

                case Keys.Delete:
                    testo = "TASTO CANC / DELETE:\n\n" +
                            "• Azzera l'immissione corrente (C) o annulla una sequenza speciale a due passaggi";
                    break;

                case Keys.Back:
                    testo = "TASTO BACKSPACE:\n\n" +
                            "• Cancella l'ultima cifra digitata nel buffer";
                    break;
            }

            if (testo != null)
            {
                MessageBox.Show(testo, titolo, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }

            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (MostraAiutoTastoRapido(keyData))
            {
                return true;
            }

            if (VerificaSequenzaSM(keyData))
            {
                return true;
            }

            if (keyData == (Keys.Control | Keys.Shift | Keys.N))
            {
                InserisciNotaManuale();
                return true;
            }
            else if (keyData == Keys.Z || keyData == (Keys.Z | Keys.Shift))
            {
                if (statoVista == 0) // Zoom attivo solo in modalità compatta standard
                {
                    CizlaDimensioniCompatte();
                    return true;
                }
            }
            else if (keyData == (Keys.Control | Keys.S))
            {
                SalvaNastroSuFile();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.Delete))
            {
                PulisciNastro();
                return true;
            }
            else if (keyData == Keys.F6)
            {
                CiclaStatoVista();
                return true;
            }
            else if (keyData == Keys.F7)
            {
                ToggleSempreInPrimoPiano(!sempreInPrimoPiano);
                return true;
            }
            else if (keyData == Keys.Back)
            {
                CancellaUltimaCifra();
                return true;
            }
            else if (keyData == Keys.Delete)
            {
                AzzeraInserimentoCorrente();
                return true;
            }
            else if (keyData == Keys.N ||
                     ((keyData & Keys.KeyCode) == Keys.Oem7 && (keyData & Keys.Alt) == Keys.Alt) ||
                     keyData == (Keys.Oem7 | Keys.Control | Keys.Alt) ||
                     keyData == (Keys.D3 | Keys.Shift))
            {
                StampaRiferimentoDataOra();
                return true;
            }
            else if (keyData == (Keys.K | Keys.Shift))
            {
                GestisciCalcoloLordoInverso();
                return true;
            }
            else if (keyData == Keys.K)
            {
                GestisciCalcoloScontoX10();
                return true;
            }
            else if (keyData == (Keys.I | Keys.Shift))
            {
                GestisciScorporoDueAliquote();
                return true;
            }
            else if (keyData == Keys.Multiply || keyData == Keys.X)
            {
                ImpostaOperazioneMoltiplicazione();
                return true;
            }
            else if (keyData == Keys.Divide || keyData == Keys.OemQuestion)
            {
                ImpostaOperazioneDivisione();
                return true;
            }
            else if (keyData == Keys.R)
            {
                CalcolaRadiceQuadrata();
                return true;
            }
            else if (keyData == Keys.P)
            {
                CalcolaPercentualeSemplice();
                return true;
            }
            else if (keyData == Keys.D)
            {
                CalcolaDeltaPercentuale();
                return true;
            }
            else if (keyData == Keys.F1)
            {
                MostraGuidaTasti();
                return true;
            }
            else if (keyData == Keys.Up || keyData == Keys.Down)
            {
                AvanzaNastroCarta();
                return true;
            }
            else if ((keyData >= Keys.D0 && keyData <= Keys.D9) || (keyData >= Keys.NumPad0 && keyData <= Keys.NumPad9))
            {
                string cifra = keyData.ToString().Replace("D", "").Replace("NumPad", "");
                DigitaCifra(cifra[0]);
                return true;
            }
            else if (keyData == Keys.Oemcomma || keyData == Keys.Decimal)
            {
                DigitaCifra(',');
                return true;
            }
            else if (keyData == (Keys.Add | Keys.Shift) || keyData == (Keys.Oemplus | Keys.Shift))
            {
                OperazionePiuT2();
                return true;
            }
            else if (keyData == Keys.Add || keyData == Keys.Oemplus)
            {
                OperazionePiuT1();
                return true;
            }
            else if (keyData == (Keys.Subtract | Keys.Shift) || keyData == (Keys.OemMinus | Keys.Shift))
            {
                OperazioneMenoT2();
                return true;
            }
            else if (keyData == Keys.Subtract || keyData == Keys.OemMinus)
            {
                OperazioneMenoT1();
                return true;
            }
            else if (keyData == (Keys.Return | Keys.Shift))
            {
                CalcolaTotaleT2();
                return true;
            }
            else if (keyData == Keys.Return)
            {
                CalcolaTotaleT1();
                return true;
            }
            else if (keyData == (Keys.Space | Keys.Shift))
            {
                CalcolaSubtotaleT2();
                return true;
            }
            else if (keyData == Keys.Space)
            {
                CalcolaSubtotaleT1();
                return true;
            }
            else if (keyData == Keys.G)
            {
                RichiamaGT();
                return true;
            }
            else if (keyData == Keys.T)
            {
                CalcolaScatoleIVA(true);
                return true;
            }
            else if (keyData == (Keys.T | Keys.Shift))
            {
                CalcolaScatoleIVA(false);
                return true;
            }
            else if (keyData == Keys.F2)
            {
                if (rdoDecStd.Checked) rdoDecAdd.Checked = true; else rdoDecStd.Checked = true;
                return true;
            }
            else if (keyData == Keys.F3)
            {
                chkACC.Checked = !chkACC.Checked;
                return true;
            }
            else if (keyData == Keys.F4)
            {
                chkIC.Checked = !chkIC.Checked;
                return true;
            }
            else if (keyData == Keys.F5)
            {
                if (rdoArr0.Checked) rdoArr5.Checked = true;
                else if (rdoArr5.Checked) rdoArr9.Checked = true;
                else rdoArr0.Checked = true;
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool VerificaSequenzaSM(Keys keyData)
        {
            DateTime ora = DateTime.Now;

            if (keyData == Keys.S)
            {
                ultimoTastoPremuto = Keys.S;
                ultimoTastoPremutoTempo = ora;
                return false;
            }

            if (keyData == Keys.M && ultimoTastoPremuto == Keys.S && (ora - ultimoTastoPremutoTempo).TotalMilliseconds <= 1500)
            {
                ultimoTastoPremuto = Keys.None;

                if (bufferInput == "1965" || lblDisplay.Text == "1965" || displayValore == 1965m)
                {
                    EseguiEasterEggSandroMancini();
                    return true;
                }
            }

            return false;
        }

        private void EseguiEasterEggSandroMancini()
        {
            txtNastro.AppendText("   Pascalina++ minimal" + Environment.NewLine);
            txtNastro.AppendText("   Black Cat Software" + Environment.NewLine);
            txtNastro.AppendText("   © 2026 Sandro Mancini" + Environment.NewLine);
            AvanzaNastroCarta();
        }

        private void GestisciCalcoloScontoX10()
        {
            decimal valore = OttieniValoreIngresso();

            if (baseLordoSpeciale == 0)
            {
                if (valore <= 0) return;

                baseLordoSpeciale = valore;
                StampaNastro($"{baseLordoSpeciale:N2}", "BAS");
                ResetPerNuovaOperazione();
            }
            else
            {
                decimal netto = valore;

                if (netto <= 0)
                {
                    MessageBox.Show("Inserire un valore netto valido maggiore di zero.", "Valore non valido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal rapporto = netto / (baseLordoSpeciale * 0.90m);
                decimal scontoX = (1.0m - rapporto) * 100m;
                decimal scontoArrotondato = Arrotonda(scontoX);

                StampaNastro($"{netto:N2}", "NET");
                StampaNastro($"{scontoArrotondato:N2}", "%X10");

                lblDisplay.Text = $"{scontoArrotondato:N2}";
                displayValore = scontoArrotondato;

                baseLordoSpeciale = 0;
                ResetPerNuovaOperazione();
            }
        }

        private void GestisciCalcoloLordoInverso()
        {
            decimal valore = OttieniValoreIngresso();

            if (nettoInversoSpeciale == 0)
            {
                if (valore <= 0) return;

                nettoInversoSpeciale = valore;
                StampaNastro($"{nettoInversoSpeciale:N2}", "NET");
                ResetPerNuovaOperazione();
            }
            else
            {
                decimal percentualeY = valore;

                if (percentualeY >= 100m)
                {
                    MessageBox.Show("La percentuale di sconto deve essere inferiore a 100.", "Errore Valore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal lordo = (nettoInversoSpeciale / 0.90m) / ((100m - percentualeY) / 100m);
                decimal lordoArrotondato = Arrotonda(lordo);

                StampaNastro($"{percentualeY:N2} %", "%SC");
                StampaNastro($"{lordoArrotondato:N2}", "LOR");

                lblDisplay.Text = $"{lordoArrotondato:N2}";
                displayValore = lordoArrotondato;

                nettoInversoSpeciale = 0;
                ResetPerNuovaOperazione();
            }
        }

        private void GestisciScorporoDueAliquote()
        {
            decimal valore;
            if (string.IsNullOrWhiteSpace(bufferInput) || nuovaImmissione)
            {
                valore = registroPrecedente != 0 ? registroPrecedente : displayValore;
            }
            else
            {
                valore = OttieniValoreIngresso();
            }

            if (imponibileScorporo == 0)
            {
                if (valore <= 0) return;

                imponibileScorporo = valore;
                StampaNastro($"{imponibileScorporo:N2}", "IMP");
                ResetPerNuovaOperazione();
            }
            else
            {
                decimal totaleLordo = OttieniValoreIngresso();

                // Limiti matematici: il lordo deve essere compreso tra il 10% e il 22% di IVA
                decimal lordoMinimo = imponibileScorporo * 1.10m;
                decimal lordoMassimo = imponibileScorporo * 1.22m;

                if (totaleLordo < lordoMinimo || totaleLordo > lordoMassimo)
                {
                    MessageBox.Show(
                        $"Valori incompatibili con aliquote 10% e 22%.\nPer un imponibile di {imponibileScorporo:N2}, il lordo deve essere compreso tra {lordoMinimo:N2} e {lordoMassimo:N2}.",
                        "Errore Dati Scorporo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Formula esatta del sistema lineare
                decimal imp22 = (totaleLordo - (1.10m * imponibileScorporo)) / 0.12m;
                decimal imp10 = imponibileScorporo - imp22;

                imp22 = Arrotonda(imp22);
                imp10 = Arrotonda(imp10);

                StampaNastro($"{totaleLordo:N2}", "LOR");
                StampaNastro($"{imp22:N2}", "I22");
                StampaNastro($"{imp10:N2}", "I10");

                lblDisplay.Text = $"{imp22:N2}";
                displayValore = imp22;
                registroPrecedente = imp22;

                imponibileScorporo = 0;
                ResetPerNuovaOperazione();
            }
        }

        private void EseguiOperazioneInAttesaSePresente()
        {
            if (operazioneInAttesa == "*" || operazioneInAttesa == "/")
            {
                decimal valCorrente = OttieniValoreIngresso();
                StampaNastro($"{valCorrente:N2}", "=");

                if (operazioneInAttesa == "*")
                {
                    registroPrecedente = Arrotonda(registroPrecedente * valCorrente);
                }
                else if (operazioneInAttesa == "/")
                {
                    if (valCorrente != 0)
                    {
                        registroPrecedente = Arrotonda(registroPrecedente / valCorrente);
                    }
                    else
                    {
                        MessageBox.Show("Impossibile dividere per zero.", "Errore Matematica", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                lblDisplay.Text = $"{registroPrecedente:N2}";
                displayValore = registroPrecedente;
            }
            else
            {
                registroPrecedente = OttieniValoreIngresso();
            }
        }

        private void ImpostaOperazioneMoltiplicazione()
        {
            EseguiOperazioneInAttesaSePresente();
            operazioneInAttesa = "*";
            StampaNastro($"{registroPrecedente:N2}", "x");
            ResetPerNuovaOperazione();
        }

        private void ImpostaOperazioneDivisione()
        {
            EseguiOperazioneInAttesaSePresente();
            operazioneInAttesa = "/";
            StampaNastro($"{registroPrecedente:N2}", "÷");
            ResetPerNuovaOperazione();
        }

        private void CancellaUltimaCifra()
        {
            if (!nuovaImmissione && bufferInput.Length > 0)
            {
                bufferInput = bufferInput.Substring(0, bufferInput.Length - 1);

                if (bufferInput.Length == 0 || bufferInput == "-")
                {
                    bufferInput = "";
                    displayValore = 0;
                    lblDisplay.Text = "0";
                    nuovaImmissione = true;
                }
                else
                {
                    if (decimal.TryParse(bufferInput, out decimal val))
                    {
                        displayValore = val;
                        lblDisplay.Text = bufferInput;
                    }
                }
            }
        }

        private void AzzeraInserimentoCorrente()
        {
            bufferInput = "";
            displayValore = 0;
            baseLordoSpeciale = 0;
            nettoInversoSpeciale = 0;
            imponibileScorporo = 0;
            lblDisplay.Text = "0";
            nuovaImmissione = true;
        }

        private void CalcolaRadiceQuadrata()
        {
            decimal val = OttieniValoreIngresso();

            if (val < 0)
            {
                MessageBox.Show("Impossibile calcolare la radice quadrata di un numero negativo.", "Errore Matematica", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double risDouble = Math.Sqrt((double)val);
            decimal risultato = Arrotonda((decimal)risDouble);

            StampaNastro($"{val:N2}", "   ");
            StampaNastro($"{risultato:N2}", " √");

            lblDisplay.Text = $"{risultato:N2}";
            ResetPerNuovaOperazione();
        }

        private void CalcolaPercentualeSemplice()
        {
            decimal percentuale = OttieniValoreIngresso();
            decimal baseCalcolo = registroPrecedente != 0 ? registroPrecedente : displayValore;

            if (baseCalcolo != 0)
            {
                decimal quotaPercentuale = baseCalcolo * (percentuale / 100m);

                StampaNastro($"{percentuale:N2} %", " %");
                StampaNastro($"{Arrotonda(quotaPercentuale):N2}", "   ");

                registroPrecedente = baseCalcolo;
                displayValore = quotaPercentuale;
                lblDisplay.Text = $"{Arrotonda(quotaPercentuale):N2}";
                operazioneInAttesa = "%";
            }

            ResetPerNuovaOperazione();
        }

        // --- TOTALIZZATORE 1 ---
        private void OperazionePiuT1()
        {
            if (operazioneInAttesa == "%")
            {
                decimal maggiorazione = displayValore;
                decimal risultatoFinale = registroPrecedente + maggiorazione;

                StampaNastro($"{Arrotonda(risultatoFinale):N2}", "+%");

                lblDisplay.Text = $"{Arrotonda(risultatoFinale):N2}";
                displayValore = Arrotonda(risultatoFinale);
                registroPrecedente = displayValore;
                operazioneInAttesa = "";
                ResetPerNuovaOperazione();
                return;
            }

            decimal val = OttieniValoreIngresso();
            registroPrecedente = val;
            totalizzatore1 += val;

            if (selettoreACC) totaleGenerale += val;
            if (selettoreIC) conteggioArticoli++;

            StampaNastro($"{val:N2}", "+1");
            ResetPerNuovaOperazione();
        }

        private void OperazioneMenoT1()
        {
            if (operazioneInAttesa == "%")
            {
                decimal sconto = displayValore;
                decimal risultatoScontato = registroPrecedente - sconto;

                StampaNastro($"{Arrotonda(risultatoScontato):N2}", "-%");

                lblDisplay.Text = $"{Arrotonda(risultatoScontato):N2}";
                displayValore = Arrotonda(risultatoScontato);
                registroPrecedente = displayValore;
                operazioneInAttesa = "";
                ResetPerNuovaOperazione();
                return;
            }

            decimal val = OttieniValoreIngresso();
            registroPrecedente = val;
            totalizzatore1 -= val;

            if (selettoreACC) totaleGenerale -= val;
            if (selettoreIC) conteggioArticoli++;

            StampaNastro($"{val:N2}", "-1");
            ResetPerNuovaOperazione();
        }

        private void CalcolaSubtotaleT1()
        {
            decimal val = Arrotonda(totalizzatore1);
            StampaNastro($"{val:N2}", "S1");
            lblDisplay.Text = $"{val:N2}";
            nuovaImmissione = true;
        }

        private void CalcolaTotaleT1()
        {
            if (operazioneInAttesa == "*" || operazioneInAttesa == "/")
            {
                decimal secondoValore = OttieniValoreIngresso();
                decimal risultato = 0;

                if (operazioneInAttesa == "*")
                {
                    StampaNastro($"{secondoValore:N2}", "=");
                    risultato = registroPrecedente * secondoValore;
                }
                else if (operazioneInAttesa == "/")
                {
                    if (secondoValore == 0)
                    {
                        MessageBox.Show("Impossibile dividere per zero.", "Errore Matematica", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetPerNuovaOperazione();
                        operazioneInAttesa = "";
                        return;
                    }
                    StampaNastro($"{secondoValore:N2}", "=");
                    risultato = registroPrecedente / secondoValore;
                }

                risultato = Arrotonda(risultato);
                StampaNastro($"{risultato:N2}", "T1");

                lblDisplay.Text = $"{risultato:N2}";
                displayValore = risultato;
                registroPrecedente = risultato;
                operazioneInAttesa = "";

                if (selettoreACC) totaleGenerale += risultato;
                if (selettoreIC) conteggioArticoli++;

                ResetPerNuovaOperazione();
                return;
            }

            if (selettoreIC && conteggioArticoli > 0)
            {
                txtNastro.AppendText($"  {conteggioArticoli,18} n#" + Environment.NewLine);
                txtNastro.SelectionStart = txtNastro.TextLength;
                txtNastro.ScrollToCaret();
            }

            decimal val = Arrotonda(totalizzatore1);
            StampaNastro($"{val:N2}", "T1");
            lblDisplay.Text = $"{val:N2}";

            registroPrecedente = val;
            displayValore = val;
            totalizzatore1 = 0;
            conteggioArticoli = 0;
            ResetPerNuovaOperazione();
        }

        // --- TOTALIZZATORE 2 (SHIFT) ---
        private void OperazionePiuT2()
        {
            decimal val = OttieniValoreIngresso();
            totalizzatore2 += val;
            StampaNastro($"{val:N2}", "+2");
            ResetPerNuovaOperazione();
        }

        private void OperazioneMenoT2()
        {
            decimal val = OttieniValoreIngresso();
            totalizzatore2 -= val;
            StampaNastro($"{val:N2}", "-2");
            ResetPerNuovaOperazione();
        }

        private void CalcolaSubtotaleT2()
        {
            decimal val = Arrotonda(totalizzatore2);
            StampaNastro($"{val:N2}", "S2");
            lblDisplay.Text = $"{val:N2}";
            nuovaImmissione = true;
        }

        private void CalcolaTotaleT2()
        {
            decimal val = Arrotonda(totalizzatore2);
            StampaNastro($"{val:N2}", "T2");
            lblDisplay.Text = $"{val:N2}";

            registroPrecedente = val;
            displayValore = val;
            totalizzatore2 = 0;
            ResetPerNuovaOperazione();
        }

        private void RichiamaGT()
        {
            decimal val = Arrotonda(totaleGenerale);
            StampaNastro($"{val:N2}", "GT");
            lblDisplay.Text = $"{val:N2}";
            totaleGenerale = 0;
            ResetPerNuovaOperazione();
        }

        private void CalcolaDeltaPercentuale()
        {
            decimal valoreAttuale = OttieniValoreIngresso();

            if (registroPrecedente == 0)
            {
                registroPrecedente = valoreAttuale;
                StampaNastro($"{valoreAttuale:N2}", "  Δ");
            }
            else
            {
                decimal differenza = valoreAttuale - registroPrecedente;
                decimal percentualeDelta = (differenza / registroPrecedente) * 100m;

                StampaNastro($"{valoreAttuale:N2}", "   ");
                StampaNastro($"{differenza:N2}", " Δ=");
                StampaNastro($"{Arrotonda(percentualeDelta):N2} %", " Δ%");

                lblDisplay.Text = $"{Arrotonda(percentualeDelta):N2}";
                registroPrecedente = 0;
            }

            ResetPerNuovaOperazione();
        }

        private void CalcolaScatoleIVA(bool aggiungiIVA)
        {
            decimal baseVal = OttieniValoreIngresso();
            decimal quotaIVA;
            decimal risultato;

            if (aggiungiIVA)
            {
                // Da imponibile ad importo con IVA: Base + (Base * 22%)
                quotaIVA = Arrotonda(baseVal * (aliquotaIVA / 100m));
                risultato = baseVal + quotaIVA;

                StampaNastro($"{baseVal:N2}", "   ");
                StampaNastro($"{aliquotaIVA:N2} %", "TAX");
                StampaNastro($"{quotaIVA:N2}", "+IVA");
                StampaNastro($"{risultato:N2}", "+TAX");
            }
            else
            {
                // Da lordo a imponibile (scorporo corretto): Lordo / 1.22
                decimal moltiplicatore = 1.0m + (aliquotaIVA / 100m);
                risultato = Arrotonda(baseVal / moltiplicatore);
                quotaIVA = baseVal - risultato;

                StampaNastro($"{baseVal:N2}", "LOR");
                StampaNastro($"{aliquotaIVA:N2} %", "TAX");
                StampaNastro($"{quotaIVA:N2}", "-IVA");
                StampaNastro($"{risultato:N2}", "-TAX");
            }

            lblDisplay.Text = $"{risultato:N2}";
            displayValore = risultato;
            registroPrecedente = risultato;
            ResetPerNuovaOperazione();
        }

        private decimal Arrotonda(decimal valore)
        {
            return modalitaArrotondamento switch
            {
                0 => Math.Truncate(valore * 100m) / 100m,
                9 => Math.Ceiling(valore * 100m) / 100m,
                _ => Math.Round(valore, 2, MidpointRounding.AwayFromZero)
            };
        }

        private void StampaNastro(string valore, string simbolo)
        {
            txtNastro.AppendText($"  {valore,18} {simbolo,-5}" + Environment.NewLine);
            txtNastro.SelectionStart = txtNastro.TextLength;
            txtNastro.ScrollToCaret();
        }

        private void ResetPerNuovaOperazione()
        {
            bufferInput = "";
            nuovaImmissione = true;
        }

        private void MostraGuidaTasti()
        {
            string nl = "\r\n";
            string messaggio =
                "=== GUIDA MAPPATURA TASTI PASCALINA++ ===" + nl + nl +
                "GUIDA RAPIDA TASTI (ALT + TASTO):" + nl +
                "  Premi ALT assieme a qualsiasi tasto (es. Alt+K, Alt+T, Alt+P) per vederne la spiegazione rapida." + nl + nl +
                "GESTIONE VISTA E FINESTRA:" + nl +
                "  F6             : Cicla Vista Compatta -> Super Compatta (6 righe) -> Estesa" + nl +
                "  Z              : Cicla ingrandimento Vista Compatta (100% -> 120% -> 150%)" + nl +
                "  F7             : Attiva/Disattiva 'Sempre in Primo Piano' (Display azzurro)" + nl +
                "  Ctrl + S       : Salva il nastro su file .txt" + nl +
                "  Ctrl + Delete  : Azzera e pulisci il nastro" + nl +
                "  Freccia Su/Giù : Avanza nastro carta (riga vuota)" + nl + nl +
                "CANCELLAZIONE E CORREZIONE:" + nl +
                "  Backspace (⌫)  : Elimina l'ultima cifra digitata" + nl +
                "  Canc (Delete)  : Azzera inserimento corrente (C) o annulla calcolo speciale attivo" + nl + nl +
                "TOTALIZZATORE 1 (T1):" + nl +
                "  Numpad +       : Aggiungi (+1)" + nl +
                "  Numpad -       : Sottrai (-1)" + nl +
                "  Spazio         : Subtotale S1" + nl +
                "  Invio          : Totale T1 (=)" + nl + nl +
                "TOTALIZZATORE 2 (T2 - TASTI SHIFT):" + nl +
                "  Shift + +      : Aggiungi al registro 2 (+2)" + nl +
                "  Shift + -      : Sottrai dal registro 2 (-2)" + nl +
                "  Shift + Spazio : Subtotale S2" + nl +
                "  Shift + Invio  : Totale T2" + nl + nl +
                "OPERAZIONI ARITMETICHE E PERCENTUALI:" + nl +
                "  * oppure X     : Moltiplicazione (x)" + nl +
                "  /              : Divisione (÷)" + nl +
                "  P              : Percentuale Olivetti (%):" + nl +
                "                   1. Inserisci Base e premi * (oppure usa totale T1)" + nl +
                "                   2. Digita la % e premi P (%)" + nl +
                "                   3. Premi + per maggiorare (+%) o - per scontare (-%)" + nl +
                "  D              : Delta percentuale (Δ)" + nl +
                "  R              : Radice quadrata (√)" + nl +
                "  G              : Totale Generale (GT)" + nl + nl +
                "CALCOLI SPECIALI, NON-ADD E NOTE (PANNELLO A SINISTRA):" + nl +
                "  Ctrl+Shift+N   : Inserisci Nota Manuale descrittiva (*, max 24 car.)" + nl +
                "  # / N          : Non-Add / Data e Ora:" + nl +
                "                   - a vuoto: stampa data e ora correnti (#D)" + nl +
                "                   - con cifre: stampa numero di rif./codice senza sommare (#)" + nl +
                "  K              : Ricavo Sconto X su schema (X + 10%):" + nl +
                "                   - 1° tocco: Lordo/Base (BAS)" + nl +
                "                   - 2° tocco: Netto (NET) -> calcola % sconto X (%X10)" + nl +
                "  Shift + K      : Ricavo Lordo Inverso da schema (X + 10%):" + nl +
                "                   - 1° tocco: Netto (NET)" + nl +
                "                   - 2° tocco: % Sconto Y (%SC) -> ricava Lordo originario (LOR)" + nl +
                "  Shift + I      : Scorporo Due Aliquote (22% e 10%):" + nl +
                "                   - 1° tocco: Imponibile Totale (IMP)" + nl +
                "                   - 2° tocco: Totale Lordo Fattura (LOR) ->" + nl +
                "                     stampa Imponibile 22% (I22) e Imponibile 10% (I10)" + nl + nl +
                "CALCOLO IVA:" + nl +
                "  T              : Calcola e Aggiungi IVA (+TAX 22%)" + nl +
                "  Shift + T      : Scomputa IVA (-TAX)" + nl + nl +
                "SELETTORI E MODALITÀ:" + nl +
                "  F1 : Guida  |  F2 : Decimali  |  F3 : ACC  |  F4 : IC  |  F5 : Arrotondamento" + nl;

            using (Form frmGuida = new Form())
            {
                frmGuida.Text = "Mappatura Tasti e Funzioni";
                frmGuida.ClientSize = new Size(640, 520);
                frmGuida.StartPosition = FormStartPosition.CenterParent;
                frmGuida.FormBorderStyle = FormBorderStyle.FixedDialog;
                frmGuida.MaximizeBox = false;
                frmGuida.MinimizeBox = false;
                frmGuida.ShowInTaskbar = false;

                frmGuida.TopMost = this.TopMost;
                frmGuida.Owner = this;

                TextBox txtInfo = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    WordWrap = false,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10f),
                    BackColor = Color.White,
                    Text = messaggio,
                    SelectionStart = 0
                };

                frmGuida.Controls.Add(txtInfo);
                frmGuida.ShowDialog(this);
            }
        }

        private void SalvaNastroSuFile()
        {
            if (string.IsNullOrWhiteSpace(txtNastro.Text))
            {
                MessageBox.Show("Il nastro di stampa è vuoto.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "File di testo (*.txt)|*.txt";
                dialog.Title = "Salva Nastro di Stampa";
                dialog.FileName = $"Nastro_Olivetti_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, txtNastro.Text);
                    MessageBox.Show("Nastro di stampa salvato con successo!", "Salvataggio Completo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void PulisciNastro()
        {
            if (txtNastro.TextLength > 0)
            {
                DialogResult esito = MessageBox.Show(
                    "Vuoi cancellare lo storico sul nastro di stampa?",
                    "Pulisci Nastro",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (esito == DialogResult.Yes)
                {
                    txtNastro.Clear();
                    lblDisplay.Text = "0";
                    displayValore = 0;
                    registroPrecedente = 0;
                    baseLordoSpeciale = 0;
                    nettoInversoSpeciale = 0;
                    imponibileScorporo = 0;
                    operazioneInAttesa = "";
                    ResetPerNuovaOperazione();
                }
            }
        }

        private void AvanzaNastroCarta()
        {
            txtNastro.AppendText(Environment.NewLine);
            txtNastro.SelectionStart = txtNastro.TextLength;
            txtNastro.ScrollToCaret();
        }

        private void DigitaCifra(char c)
        {
            if (nuovaImmissione)
            {
                bufferInput = "";
                nuovaImmissione = false;
            }

            if (c == ',' && !bufferInput.Contains(","))
            {
                bufferInput += ",";
            }
            else if (char.IsDigit(c) && bufferInput.Length < 14)
            {
                bufferInput += c;
            }

            if (decimal.TryParse(bufferInput, out decimal val))
            {
                displayValore = val;
                lblDisplay.Text = bufferInput;
            }
        }

        private decimal OttieniValoreIngresso()
        {
            if (selettoreDecimaliAddizione && !bufferInput.Contains(","))
            {
                return displayValore / 100m;
            }
            return displayValore;
        }
    }
}