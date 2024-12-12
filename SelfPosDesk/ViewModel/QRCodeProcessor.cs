using AForge.Video.DirectShow;
using AForge.Video;
using IronBarCode;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Diagnostics;

public class QRCodeProcessor
{
    private FilterInfoCollection _videoDevices;
    private VideoCaptureDevice _videoSource;

    // 이벤트 정의: QR 코드 스캔 데이터 전달
    public event EventHandler<string> QRCodeScanned;

    // 이벤트 정의: 카메라 프레임 업데이트
    public event EventHandler<BitmapImage> CameraFrameUpdated;

    public QRCodeProcessor()
    {
        // 웹캠 장치 초기화
        _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
    }

    public void StartCamera()
    {
        if (_videoDevices.Count == 0)
        {
            Debug.WriteLine("웹캠 없음");
            throw new Exception("웹캠이 연결되지 않았습니다.");
        }

        Debug.WriteLine("웹캠 연결 시도...");
        _videoSource = new VideoCaptureDevice(_videoDevices[0].MonikerString);
        _videoSource.NewFrame += OnNewFrame;
        _videoSource.Start();

        if (_videoSource.IsRunning)
        {
            Debug.WriteLine("카메라 스트림 시작 성공");
        }
        else
        {
            Debug.WriteLine("카메라 스트림 시작 실패");
        }
    }

    public void StopCamera()
    {
        if (_videoSource != null && _videoSource.IsRunning)
        {
            _videoSource.NewFrame -= OnNewFrame; // 이벤트 해제
            _videoSource.SignalToStop();
            _videoSource = null;
        }
    }
    private void OnNewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();

        // QR 코드 스캔 디버깅 로그
        Debug.WriteLine($"QR 코드 스캔 시도 중... Bitmap 크기 = {bitmap.Width}x{bitmap.Height}");

        // 디버깅용으로 Bitmap 저장
        SaveBitmapForDebug(bitmap);

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                // BitmapImage 변환 및 UI 바인딩
                BitmapImage frameImage = BitmapToBitmapImage(bitmap);
                if (frameImage != null)
                {
                    CameraFrameUpdated?.Invoke(this, frameImage);
                }

                // QR 코드 읽기 시도
                var results = BarcodeReader.Read(bitmap);
                if (results != null && results.Any())
                {
                    Debug.WriteLine($"QR 코드 스캔 성공: {results.First().Value}");
                    QRCodeScanned?.Invoke(this, results.First().Value);
                }
                else
                {
                    Debug.WriteLine("QR 코드 스캔 실패: 결과 없음");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNewFrame 처리 중 오류 발생: {ex.Message}");
            }
        });
    }

    // 디버깅용 Bitmap 저장 메서드
    private void SaveBitmapForDebug(Bitmap bitmap)
    {
        try
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "debug_frame.bmp");

            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Bmp);
            Debug.WriteLine($"디버깅용 Bitmap 저장 성공: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"디버깅용 Bitmap 저장 실패: {ex.Message}");
        }
    }

    private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
    {
        try
        {
            using (Bitmap newBitmap = new Bitmap(bitmap))
            {
                newBitmap.SetResolution(300, 300); // DPI를 높여 해상도 개선
                using (MemoryStream memory = new MemoryStream())
                {
                    newBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    Debug.WriteLine("Bitmap 변환 성공 (300 DPI)");
                    return bitmapImage;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Bitmap 변환 실패: {ex.Message}");
            return null;
        }
    }
}
