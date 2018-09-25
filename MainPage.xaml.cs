using Microsoft.Advertising.WinRT.UI;
using SongsMatchGame.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace SongsMatchGame
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<StorageFile> AllSongs;
        private ObservableCollection<Song> Songs;
        private bool musicPlay_bool = false;
        private int num = 1;
        private int total_score;
        private int score;

        InterstitialAd myInterstitialAd = null;
        string myAppId = "d25517cb-12d4-4699-8bdc-52040c712cab";
        string myAdUnitId = "test";
        public MainPage()
        {
            this.InitializeComponent();
            Songs = new ObservableCollection<Song>();
            myInterstitialAd = new InterstitialAd();
            myInterstitialAd.AdReady += MyInterstitialAd_AdReady;
            myInterstitialAd.ErrorOccurred += MyInterstitialAd_ErrorOccurred;
            myInterstitialAd.Completed += MyInterstitialAd_Completed;
            myInterstitialAd.Cancelled += MyInterstitialAd_Cancelled;
        }

        private async Task GetAllSongs(ObservableCollection<StorageFile> list, StorageFolder folder)
        {
            foreach (var song in await folder.GetFilesAsync())
            {
                if (song.FileType == ".mp3")
                {
                    list.Add(song);
                }
            }
            foreach (var item in await folder.GetFoldersAsync())
            {
                await GetAllSongs(list, item);
            }
        }
        private async Task<List<StorageFile>> GetRandomSongs(ObservableCollection<StorageFile> allsongs)
        {

            Random songRd = new Random();
            List<StorageFile> listsongs = new List<StorageFile>();
            var allsongsCount = allsongs.Count;
            while (listsongs.Count < 10)
            {
                bool music_bool = false;
                var randomNum = songRd.Next(allsongsCount);
                var randomsong = allsongs[randomNum];
                MusicProperties randomsongProperties = await randomsong.Properties.GetMusicPropertiesAsync();
                foreach (var song in listsongs)
                {
                    MusicProperties listsongsProperties = await song.Properties.GetMusicPropertiesAsync();
                    if (String.IsNullOrEmpty(randomsongProperties.Album) || listsongsProperties.Album == randomsongProperties.Album)
                    {
                        music_bool = true;
                    }
                }
                if (!music_bool)
                {
                    listsongs.Add(randomsong);
                }
            }
            return listsongs;
        }
        private async Task GameSongs(List<StorageFile> files)
        {
            int id = 0;
            foreach (var song in files)
            {
                MusicProperties song_Properties = await song.Properties.GetMusicPropertiesAsync();
                StorageItemThumbnail current_Thumb = await song.GetThumbnailAsync(
                    ThumbnailMode.MusicView,
                    200,
                    ThumbnailOptions.UseCurrentScale);
                BitmapImage Album_Cover = new BitmapImage();
                Album_Cover.SetSource(current_Thumb);

                Song newsongs = new Song();
                newsongs.Id = id;
                newsongs.Title = song_Properties.Title;
                newsongs.Artist = song_Properties.Artist;
                newsongs.Album = song_Properties.Album;
                newsongs.AlbumCover = Album_Cover;
                newsongs.SongFile = song;
                newsongs.Used = false;
                Songs.Add(newsongs);
                id++;
            }
        }

        private async void main_gridview_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (musicPlay_bool == false) return;
            main_mediaElement.Stop();
            main_storyBoard.Pause();
            var click_songs = (Song)e.ClickedItem;
            Uri uri;
            if (click_songs.Selected)
            {
                uri = new Uri("ms-appx:///Assets/correct.png");
                score = (int)main_progressbar.Value;

            }
            else
            {
                uri = new Uri("ms-appx:///Assets/incorrect.png");
                score = (int)main_progressbar.Value * -1;
            }
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var file_stream = await file.OpenAsync(FileAccessMode.Read);
            await click_songs.AlbumCover.SetSourceAsync(file_stream);
            ClickSameMethod();
            click_songs.Used = true;

        }
        private void ClickSameMethod()
        {
            total_score += score;
            var correct_song = Songs.FirstOrDefault(p => p.Selected == true);
            score_textlbock.Text = $"The score: {score}, Total score: {total_score} after {num} rounds";
            title_textlbock.Text = $"Correct song Title: {correct_song.Title}";
            artist_textlbock.Text = $"Correct song Artist: {correct_song.Artist}";
            album_textlbock.Text = $"Correct song Album: {correct_song.Album}";
            if (num < 5)
            {
                num++;
                WaitingStart();
            }
            else
            {
                reminder_textblock.Text = "Game Over";
                main_storyBoard.Pause();
                refresh_button.Visibility = Visibility.Visible;
            }
            if (num == 5)
            {
                myInterstitialAd.RequestAd(AdType.Video, myAppId, myAdUnitId);
            }
            correct_song.Used = true;
            correct_song.Selected = false;
        }
        private void Page_Loaded_1(object sender, RoutedEventArgs e)
        {
            main_progressbar.Visibility = Visibility.Collapsed;
            UseMethod();
            refresh_button.Visibility = Visibility.Collapsed;

        }

        private async void UseMethod()
        {
            main_progressdRing.IsActive = true;

            StorageFolder folder = KnownFolders.MusicLibrary;
            AllSongs = new ObservableCollection<StorageFile>();
            await GetAllSongs(AllSongs, folder);
            Songs.Clear();
            var randomsongs = await GetRandomSongs(AllSongs);
            await GameSongs(randomsongs);
            WaitingStart();
            main_progressdRing.IsActive = false;
            main_progressbar.Visibility = Visibility.Visible;
        }
        private void refresh_button_Click(object sender, RoutedEventArgs e)
        {
            if (InterstitialAdState.Ready == myInterstitialAd.State)
            {
                myInterstitialAd.Show();
            }
        }

        async void MyInterstitialAd_AdReady(object sender, object e)
        {
            // Your code goes here.
            //ContentDialog content = new ContentDialog
            //{
            //    Title = "error",
            //    Content = "Please wait seconds",
            //    IsPrimaryButtonEnabled = true,
            //    PrimaryButtonText = "OK",
            //};
            //ContentDialogResult value = await content.ShowAsync();
        }

        void MyInterstitialAd_ErrorOccurred(object sender, AdErrorEventArgs e)
        {
            // Your code goes here.
        }

        void MyInterstitialAd_Completed(object sender, object e)
        {
            // Your code goes here.
            reminder_textblock.Text = "";
            main_progressbar.Visibility = Visibility.Collapsed;
            refresh_button.Visibility = Visibility.Collapsed;
            UseMethod();
            num = 1;
            total_score = 0;
            score_textlbock.Text = "";
            title_textlbock.Text = "";
            artist_textlbock.Text = "";
            album_textlbock.Text = "";
        }

        void MyInterstitialAd_Cancelled(object sender, object e)
        {
            // Your code goes here.
            myInterstitialAd.RequestAd(AdType.Video, myAppId, myAdUnitId);
        }
        private void main_storyBoard_Completed(object sender, object e)
        {
            if (musicPlay_bool)
            {
                score = -100;
                main_mediaElement.Stop();
                ClickSameMethod();
            }
            else
            {
                StartGuessing();
                GetPlaySong();
            }
        }
        private void WaitingStart()
        {
            musicPlay_bool = false;
            SolidColorBrush color = new SolidColorBrush(Colors.LightBlue);
            main_progressbar.Foreground = color;
            reminder_textblock.Foreground = color;
            reminder_textblock.Text = $"Get ready for round {num}...";
            main_storyBoard.Begin();
        }
        private void StartGuessing()
        {
            musicPlay_bool = true;
            SolidColorBrush color = new SolidColorBrush(Colors.Red);
            main_progressbar.Foreground = color;
            reminder_textblock.Foreground = color;
            reminder_textblock.Text = "Beginning Guessing...";
            main_storyBoard.Begin();
        }
        private async void GetPlaySong()
        {
            Random rd = new Random();
            var unUsedSongs = Songs.Where(p => p.Used == false);
            int index = rd.Next(unUsedSongs.Count());
            var thisRandomSong = unUsedSongs.ElementAt(index);
            thisRandomSong.Selected = true;

            main_mediaElement.SetSource(await thisRandomSong.SongFile.OpenAsync(FileAccessMode.Read),
                thisRandomSong.SongFile.ContentType);
            thisRandomSong.Used = true;
        }
    }
}
