using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SelfPosDesk
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaPlayer media;
        private string[] musicFiles;
        private int currentTrackIndex;

        public MainWindow()
        {
            InitializeComponent();

            // Closing 이벤트 구독
            this.Closing += OnWindowClosing;

            // media 초기화
            media = new MediaPlayer();

            // 음악 파일 목록 설정
            musicFiles = new string[]
            { @"C:\Users\lms105\Desktop\project_soiso\ProjectSoiso\SelfPosDesk\music\cmMusic.mp3", @"C:\Users\lms105\Desktop\project_soiso\ProjectSoiso\SelfPosDesk\music\music.mp3"};

            currentTrackIndex = 0;

            // 첫 번째 음악 파일 로드 및 재생
            media.Open(new Uri(musicFiles[currentTrackIndex]));
            media.MediaEnded += MediaPlayer_MediaEnded;
            media.Play();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // 강제로 애플리케이션 종료 (필요 시)
            System.Environment.Exit(0);
        }

        // 음악이 끝날 때마다 호출되어 다음 음악 파일로 넘어감
        public void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            // 다음 곡의 인덱스를 계산
            currentTrackIndex = (currentTrackIndex + 1) % musicFiles.Length;

            // 다음 음악 파일을 로드하고 재생
            media.Open(new Uri(musicFiles[currentTrackIndex]));
            media.Play();
        }

        // 창 닫힐 때 음악 중지
        protected override void OnClosed(EventArgs e)
        {
            media.Close();
            base.OnClosed(e);
        }
    }
}
