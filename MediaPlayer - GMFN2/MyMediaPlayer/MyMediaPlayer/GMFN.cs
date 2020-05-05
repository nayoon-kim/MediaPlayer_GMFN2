using System;
using System.Drawing;
using System.Drawing.Imaging;
using Python.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using Torch;
using Numpy.Models;
using Numpy;

namespace MyMediaPlayer
{
    public unsafe class GMFN
    {
        public dynamic DL;
        public dynamic opt, solver;

        public GMFN()
        {
            var pythonPath = @"C:\Users\BONITO\Anaconda3";

            Environment.SetEnvironmentVariable("PATH", $@"{pythonPath};" + Environment.GetEnvironmentVariable("PATH"));
            Environment.SetEnvironmentVariable("PYTHONHOME", pythonPath);
            Environment.SetEnvironmentVariable("PYTHONPATH ", $@"{pythonPath}\Lib");

            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();

            TestRunning();
        }

        private void TestRunning()
        {
            Bitmap bitmap = new Bitmap(@"./GMFN/bird.bmp");
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;

            int size = Math.Abs(bmpData.Stride) * bitmap.Height;
            byte[] array = new byte[size];
            //byte[] array_x4 = new byte[size * 16];

            Marshal.Copy(ptr, array, 0, size);
            bitmap.UnlockBits(bmpData);

            using (Py.GIL())
            {
                dynamic test = Py.Import("GMFN.gmfn_test");
                DL = test.SR(@"./GMFN/test_GMFN_example.json");

                Tensor _tensor = torch.randn(new Shape(size));
                _tensor = array;
                _tensor = _tensor.reshape(new Shape(new int[] { bmpData.Height, bmpData.Width, 3 }));
                _tensor = _tensor.transpose(0, 2).transpose(1, 2);
                _tensor = _tensor.unsqueeze(0);
                DL.TensorToList(_tensor);

                Stopwatch sw = new Stopwatch();
                sw.Start();
                Tensor batch = new Tensor(DL.images_bmp(array, bmpData.Width, bmpData.Height, bmpData.Stride));
                sw.Stop();
                Console.WriteLine("data ms: " + sw.ElapsedMilliseconds.ToString() + "ms");

            }

        }
        public Bitmap SR(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            
            int size = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] array = new byte[size];
            byte[] array_x4 = new byte[size * 16];

            Marshal.Copy(ptr, array, 0, size);
            bmp.UnlockBits(bmpData);


            using (Py.GIL())
            {

                Stopwatch sw1 = new Stopwatch();
                sw1.Start();
                //Tensor _tensor = torch.randn(new Shape(size));
                //_tensor = array;
                //_tensor = _tensor.reshape(new Shape(new int[] { bmpData.Height, bmpData.Width, 3 }));
                //_tensor = _tensor.transpose(0, 2).transpose(1, 2);
                //_tensor = _tensor.unsqueeze(0);

                sw1.Stop();
                Console.WriteLine("data ms: " + sw1.ElapsedMilliseconds.ToString() + "ms");

                Stopwatch sw = new Stopwatch();

                sw.Start();

                // output 처리 완료
                Tensor tensor = new Tensor(DL.images_bmp(array, bmpData.Width, bmpData.Height, bmpData.Stride));
                tensor = tensor.squeeze().permute(new int[] { 1, 2, 0 }).reshape(new Shape(-1));

                sw.Stop();
                Console.WriteLine("SR ms: " + sw.ElapsedMilliseconds.ToString() + "ms");

                array_x4 = tensor.GetData<byte>();
            }
            

            Bitmap bmp_x4 = BytesToBitmap(bmpData, array_x4);
            
            return bmp_x4;
        }

        private static Bitmap BytesToBitmap(BitmapData bitmapData, byte[] data)
        {
            Bitmap bmp = new Bitmap(bitmapData.Width * 4, bitmapData.Height * 4, bitmapData.PixelFormat);

            BitmapData bmpData = bmp.LockBits(
                                 new Rectangle(0, 0, bmp.Width, bmp.Height),
                                 ImageLockMode.WriteOnly, bmp.PixelFormat);

            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

            bmp.UnlockBits(bmpData);
            return bmp;
        }
    }        
}
