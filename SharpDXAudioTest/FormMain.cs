using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharpDXAudioTest
{
    public partial class FormMain : Form
    {
        AudioState audioState;
        int frameToApply3DAudio = 0;
        TimeSpan gameTime = TimeSpan.Zero;
        bool resume = false;
        float elapsedTime = 0;

        ListenerInstance listenerInstance = null;

        VoiceInstance helicopter = null;
        EmitterInstance emitterHelicopter = null;

        VoiceInstance music = null;
        EmitterInstance emitterMusic = null;

        private class ToUpdate3DVoice
        {
            public VoiceInstance Voice { get; set; }
            public EmitterInstance Emitter { get; set; }
        }

        readonly List<ToUpdate3DVoice> voices3d = new List<ToUpdate3DVoice>();

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            InitUI();

            audioState = new AudioState(48000);

            music = audioState.InitializeVoice("MusicMono.wav");

            helicopter = audioState.InitializeVoice("heli.wav", true);

            InitAgents();

            gameTime = DateTime.Now.TimeOfDay;

            Task.Run(async () =>
            {
                bool startAudio = true;

                bool res = true;
                while (res)
                {
                    TimeSpan prevTime = gameTime;
                    gameTime = DateTime.Now.TimeOfDay;
                    elapsedTime = (float)(gameTime - prevTime).TotalSeconds;

                    if (!UpdateAudio(elapsedTime))
                    {
                        res = false;
                    }

                    UpdateText();

                    await Task.Delay(TimeSpan.FromMilliseconds(1000f / 60f));

                    if (startAudio)
                    {
                        helicopter.Start();
                        music.Start();

                        startAudio = false;
                    }
                }

                this.Close();

            }).ConfigureAwait(false);
        }
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanupAudio();
        }
        private void ButPauseResume_Click(object sender, EventArgs e)
        {
            PauseAudio(resume);
            resume = !resume;
        }
        private void CmbEffects_SelectedValueChanged(object sender, EventArgs e)
        {
            int value = cmbEffects.SelectedIndex;

            if (!helicopter.SetReverb(value))
            {
                MessageBox.Show("Set reverb error");
            }
        }
        private void ButUp_Click(object sender, EventArgs e)
        {
            var pos = chkMove.Checked ? listenerInstance.Position : emitterHelicopter.Position;
            pos.Z += 1;
            pos.Z = Math.Min(AudioConstants.ZMAX, pos.Z);
            if (chkMove.Checked)
            {
                listenerInstance.Position = pos;
            }
            else
            {
                emitterHelicopter.Position = pos;
            }
        }
        private void ButRight_Click(object sender, EventArgs e)
        {
            var pos = chkMove.Checked ? listenerInstance.Position : emitterHelicopter.Position;
            pos.X += 1;
            pos.X = Math.Min(AudioConstants.XMAX, pos.X);
            if (chkMove.Checked)
            {
                listenerInstance.Position = pos;
            }
            else
            {
                emitterHelicopter.Position = pos;
            }
        }
        private void ButDown_Click(object sender, EventArgs e)
        {
            var pos = chkMove.Checked ? listenerInstance.Position : emitterHelicopter.Position;
            pos.Z -= 1;
            pos.Z = Math.Max(AudioConstants.ZMIN, pos.Z);
            if (chkMove.Checked)
            {
                listenerInstance.Position = pos;
            }
            else
            {
                emitterHelicopter.Position = pos;
            }
        }
        private void ButLeft_Click(object sender, EventArgs e)
        {
            var pos = chkMove.Checked ? listenerInstance.Position : emitterHelicopter.Position;
            pos.X -= 1;
            pos.X = Math.Max(AudioConstants.XMIN, pos.X);
            if (chkMove.Checked)
            {
                listenerInstance.Position = pos;
            }
            else
            {
                emitterHelicopter.Position = pos;
            }
        }
        private void ChkListenerCone_CheckedChanged(object sender, EventArgs e)
        {
            listenerInstance.UseCone = !listenerInstance.UseCone;
        }
        private void ChkListenerInnerRadius_CheckedChanged(object sender, EventArgs e)
        {
            listenerInstance.UseInnerRadius = !listenerInstance.UseInnerRadius;
        }

        private void UpdateText()
        {
            var helicopterDsp = helicopter.GetMatrixCoefficients();
            var musicDsp = music.GetMatrixCoefficients();

            string helicopterText = $"Helicopter      Pos {emitterHelicopter.Position.X:00},{emitterHelicopter.Position.Y:00},{emitterHelicopter.Position.Z:00}";
            string helicopterDspText = $"Helicopter DSP.   L {helicopterDsp[0]:0.000} R {helicopterDsp[1]:0.000}";
            string musicText = $"Music           Pos {emitterMusic.Position.X:00},{emitterMusic.Position.Y:00},{emitterMusic.Position.Z:00}";
            string musicDspText = $"Music DSP.        L {musicDsp[0]:0.000} R {musicDsp[1]:0.000}";
            string listenerText = $"Listener        Pos {listenerInstance.Position.X:00},{listenerInstance.Position.Y:00},{listenerInstance.Position.Z:00}";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(listenerText);
            sb.AppendLine(helicopterText);
            sb.AppendLine(musicText);
            sb.AppendLine();
            sb.AppendLine(helicopterDspText);
            sb.AppendLine(musicDspText);

            if (this.InvokeRequired)
                this.Invoke(new MethodInvoker(delegate ()
                {
                    this.txtData.Text = sb.ToString();
                }));
            else
            {
                this.txtData.Text = sb.ToString();
            }
        }

        void InitUI()
        {
            var propNames = AudioConstants.GetPresetNames();

            this.cmbEffects.Items.AddRange(propNames.ToArray());
        }
        void InitAgents()
        {
            listenerInstance = new ListenerInstance
            {
                Position = Vector3.Zero,
            };

            emitterHelicopter = new EmitterInstance
            {
                Position = new Vector3(0f, 0f, AudioConstants.ZMAX),
            };
            helicopter.Initialize3D(audioState, emitterHelicopter, listenerInstance);
            voices3d.Add(new ToUpdate3DVoice { Voice = helicopter, Emitter = emitterHelicopter });

            emitterMusic = new EmitterInstance
            {
                Position = new Vector3(AudioConstants.XMAX, 0f, AudioConstants.ZMAX),
            };
            music.Initialize3D(audioState, emitterMusic, listenerInstance);
            voices3d.Add(new ToUpdate3DVoice { Voice = music, Emitter = emitterMusic });
        }
        bool UpdateAudio(float fElapsedTime)
        {
            try
            {
                if (!audioState.Initialized)
                {
                    return false;
                }

                if (frameToApply3DAudio < voices3d.Count)
                {
                    var voice3d = voices3d[frameToApply3DAudio];
                    voice3d.Voice.Calculate2D(fElapsedTime, listenerInstance, voice3d.Emitter);
                }

                frameToApply3DAudio++;
                frameToApply3DAudio &= voices3d.Count + 1;
            }
            catch
            {
                return false;
            }

            return true;
        }
        void PauseAudio(bool resume)
        {
            if (!audioState.Initialized)
            {
                return;
            }

            if (resume)
            {
                audioState.Device.StartEngine();
            }
            else
            {
                audioState.Device.StopEngine();
            }
        }
        void CleanupAudio()
        {
            voices3d.Clear();

            if (helicopter != null)
            {
                helicopter.Dispose();
                helicopter = null;
            }

            if (music != null)
            {
                music.Dispose();
                music = null;
            }

            if (audioState != null)
            {
                audioState.Dispose();
                audioState = null;
            }
        }
    }
}
