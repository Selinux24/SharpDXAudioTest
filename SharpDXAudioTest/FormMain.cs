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

        float masterVolume = 0;
        float musicVolume = 0;
        float helicopterVolume = 0;

        private class ToUpdate3DVoice
        {
            public VoiceInstance Voice { get; set; }
            public EmitterInstance Emitter { get; set; }
        }

        readonly List<IAgent> agents = new List<IAgent>();
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

            masterVolume = audioState.GetVolume() * 100f;
            musicVolume = music.GetVolume() * 100f;
            helicopterVolume = helicopter.GetVolume() * 100f;

            tbMasterVolume.Value = (int)masterVolume;
            tbMusic.Value = (int)musicVolume;
            tbHelicopter.Value = (int)helicopterVolume;

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
            if (audioState?.Initialized != true)
            {
                return;
            }

            int value = cbEffects.SelectedIndex;

            if (!helicopter.SetReverb(value))
            {
                MessageBox.Show("Set reverb error");
            }
        }
        private void ButUp_Click(object sender, EventArgs e)
        {
            var agent = agents.FirstOrDefault(a => a.Name == (string)cbAgent.SelectedItem);
            if (agent == null)
            {
                return;
            }

            var pos = agent.Position;
            pos.Z += 1;
            pos.Z = Math.Min(AudioConstants.ZMAX, pos.Z);
            agent.Position = pos;
        }
        private void ButRight_Click(object sender, EventArgs e)
        {
            var agent = agents.FirstOrDefault(a => a.Name == (string)cbAgent.SelectedItem);
            if (agent == null)
            {
                return;
            }

            var pos = agent.Position;
            pos.X += 1;
            pos.X = Math.Min(AudioConstants.XMAX, pos.X);
            agent.Position = pos;
        }
        private void ButDown_Click(object sender, EventArgs e)
        {
            var agent = agents.FirstOrDefault(a => a.Name == (string)cbAgent.SelectedItem);
            if (agent == null)
            {
                return;
            }

            var pos = agent.Position;
            pos.Z -= 1;
            pos.Z = Math.Max(AudioConstants.ZMIN, pos.Z);
            agent.Position = pos;
        }
        private void ButLeft_Click(object sender, EventArgs e)
        {
            var agent = agents.FirstOrDefault(a => a.Name == (string)cbAgent.SelectedItem);
            if (agent == null)
            {
                return;
            }

            var pos = agent.Position;
            pos.X -= 1;
            pos.X = Math.Max(AudioConstants.XMIN, pos.X);
            agent.Position = pos;
        }
        private void ChkListenerCone_CheckedChanged(object sender, EventArgs e)
        {
            listenerInstance.UseCone = !listenerInstance.UseCone;
        }
        private void ChkListenerInnerRadius_CheckedChanged(object sender, EventArgs e)
        {
            listenerInstance.UseInnerRadius = !listenerInstance.UseInnerRadius;
        }
        private void TbMasterVolume_Scroll(object sender, EventArgs e)
        {
            masterVolume = tbMasterVolume.Value / (float)tbMasterVolume.Maximum * 100f;

            audioState.SetVolume(masterVolume / 100f);
        }
        private void TbMusic_Scroll(object sender, EventArgs e)
        {
            musicVolume = tbMusic.Value / (float)tbMusic.Maximum * 100f;

            music.SetVolume(musicVolume / 100f);
        }
        private void TbHelicopter_Scroll(object sender, EventArgs e)
        {
            helicopterVolume = tbHelicopter.Value / (float)tbHelicopter.Maximum * 100f;

            helicopter.SetVolume(helicopterVolume / 100f);
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
            string musicVolumeText = $"Music volume      {musicVolume:000}";
            string helicopterVolumeText = $"Helicopter volume {helicopterVolume:000}";
            string masterVolumeText = $"Master volume     {masterVolume:000}";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(listenerText);
            sb.AppendLine(helicopterText);
            sb.AppendLine(musicText);
            sb.AppendLine();
            sb.AppendLine(helicopterDspText);
            sb.AppendLine(musicDspText);
            sb.AppendLine();
            sb.AppendLine(musicVolumeText);
            sb.AppendLine(helicopterVolumeText);
            sb.AppendLine(masterVolumeText);

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

            this.cbEffects.Items.AddRange(propNames.ToArray());
            this.cbEffects.SelectedIndex = 0;

            listenerInstance = new ListenerInstance
            {
                Name = "Listener",
                Position = Vector3.Zero,
            };
            emitterHelicopter = new EmitterInstance
            {
                Name = "Helicopter",
                Position = new Vector3(0f, 0f, AudioConstants.ZMAX),
            };
            emitterMusic = new EmitterInstance
            {
                Name = "Music",
                Position = new Vector3(AudioConstants.XMAX, 0f, AudioConstants.ZMAX),
            };
            agents.AddRange(new IAgent[] { listenerInstance, emitterMusic, emitterHelicopter });

            this.cbAgent.Items.AddRange(agents.Select(a => a.Name).ToArray());
            this.cbAgent.SelectedIndex = 0;
        }
        void InitAgents()
        {
            helicopter.Initialize3D(audioState, emitterHelicopter, listenerInstance);
            voices3d.Add(new ToUpdate3DVoice { Voice = helicopter, Emitter = emitterHelicopter });

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
                audioState.Start();
            }
            else
            {
                audioState.Stop();
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
