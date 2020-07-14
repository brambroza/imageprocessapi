using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Imaging;
using IronOcr;


namespace AIImageAPI.Controllers
{
    public class ImageprocessingController : ApiController
    {
        const int size = 150;
        const int quality = 75;
        // GET: api/Imageprocessing
        public IEnumerable<string> Get()
        {
            
            return new string[] { "value1", "value2" };
        }

        // GET: api/Imageprocessing/5
        public string Get(string  id)
        {
            string result;
            result = imagepro(id);
            return result;
        }

        // POST: api/Imageprocessing
        public void Post([FromBody]string value)
        {

        }

        // PUT: api/Imageprocessing/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Imageprocessing/5
        public void Delete(int id)
        {
        }


        public   string imagepro(string path)
        {
            var ocr = new AdvancedOcr()
            {
                CleanBackgroundNoise = false,
                EnhanceContrast = false,
                EnhanceResolution = false,
                Language = IronOcr.Languages.English.OcrLanguagePack,
                Strategy = IronOcr.AdvancedOcr.OcrStrategy.Fast,
                ColorSpace = AdvancedOcr.OcrColorSpace.GrayScale,
                DetectWhiteTextOnDarkBackgrounds = false,
                InputImageType = AdvancedOcr.InputTypes.Document,
                RotateAndStraighten = false,
                ReadBarCodes = false,
                ColorDepth = 4
            };


            var Resualts = ocr.Read(path);
            Console.WriteLine(Resualts.Text);
            return Resualts.Text;
        }
    }
}
