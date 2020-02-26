using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using TagRides.Shared.UserProfile;
using System.IO;

namespace TagRides.NewUser
{
    public class ProfileCreationController
    {
        UserInfo userInfo;
        Stream image = null;
        string faction;

        Page[] pages;
        NavigationPage navPage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="userInfo"></param>
        /// <param name="image">A stream for the image if one was uploaded, null otherwise</param>
        public delegate void ProfileCreated(ProfileCreationController controller, UserInfo userInfo, Stream image, string factionName);
        public event ProfileCreated ProfileCreationComplete;

        public ProfileCreationController(out Page mainPage)
        {
            userInfo = new UserInfo();

            pages = new Page[]{
                new NameInsert(this, userInfo),
                new PhoneInsert(this, userInfo),
                new PhotoInsert(this),
                //TODO Add this back in? Currently I (bdtrotte) don't like the way it works
                //new DriverInfoInsert(this, userInfo),
                new FactionInsert(this)
            };

            mainPage = navPage = new NavigationPage(pages[0]);
        }

        public async void NextPage()
        {
            int curIndex = GetCurrentIndex();

            if (curIndex == pages.Length - 1)
            {
                CreationFlowComplete();
                return;
            }

            await navPage.PushAsync(pages[curIndex + 1]);
        }

        public async void PrePage()
        {
            int curIndex = GetCurrentIndex();

            if (curIndex == 0)
                throw new Exception("Page index invalid.");

            await navPage.PushAsync(pages[curIndex - 1]);
        }

        public void SetImage(Stream image)
        {
            this.image = image;
        }

        public void SetFaction(string faction)
        {
            this.faction = faction;
        }

        public void CreationFlowComplete()
        {
            ProfileCreationComplete?.Invoke(this, userInfo, image, faction);
        }

        int GetCurrentIndex()
        {
            Page curPage = navPage.CurrentPage;

            for (int i = 0; i < pages.Length; ++i)
                if (pages[i] == curPage)
                    return i;

            return -1;
        }
    }
}
