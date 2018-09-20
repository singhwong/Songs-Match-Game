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
        public MainPage()
        {
            this.InitializeComponent();
            Songs = new ObservableCollection<Song>();
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
                    ThumbnailOptions.UseCurrentScale
                    );
                BitmapImage Album_Cover = new BitmapImage();
                Album_Cover.SetSource(current_Thumb);

                Song newsongs = new Song();
                newsongs.Id = id;
                newsongs.Title = song_Properties.Title;
                newsongs.Artist = song_Properties.Artist;
                newsongs.Album = song_Properties.Album;
                newsongs.AlbumCover = Album_Cover;
                newsongs.SongFile = song;

                Songs.Add(newsongs);
                id++;
            }
        }

        private void main_gridview_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void Page_Loaded_1(object sender, RoutedEventArgs e)
        {
            UseMethod();
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

            main_progressdRing.IsActive = false;
        }
        private void refresh_button_Click(object sender, RoutedEventArgs e)
        {
            UseMethod();
        }
    }
}
