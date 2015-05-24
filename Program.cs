using FastBitmap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AreaSmoother
{
	class Program
	{
		//Test Sqrt
		//static void Main(string[] args)
		//{
		//	for(double a=1.0; a<100.0; a+=0.1)
		//	{
		//		Console.WriteLine(Math.Sqrt(a)+" "+Sqrt(a));
		//	}
		//	Console.WriteLine(new String('=',100));
		//	for(float f=1.0f; f<100.0f; f+=0.1f)
		//	{
		//		Console.WriteLine(Math.Sqrt(f)+" "+Sqrt(f));
		//	}
		//}

		static void Main(string[] args)
		{
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
			Trace.Listeners.Add(new ConsoleTraceListener());
			if (args.Length  < 1)
			{
				Console.WriteLine("specify image file");
				return;
			}
			string filein = args[0];
			string fileout = args.Length > 1
				? args[1]
				: filein+"s.png"
			;

			var fsin = File.Open(filein,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
			Bitmap bmpin = new Bitmap(fsin);
			Bitmap bmpot = new Bitmap(bmpin.Width,bmpin.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			using(LockBitmap lbin = new LockBitmap(bmpin))
			using(LockBitmap lbot = new LockBitmap(bmpot))
			{
				lbot.LockBits();
				lbin.LockBits();
				DoSmooting(lbin,lbot);
				lbot.UnlockBits();
				lbin.UnlockBits();
			}

			bmpot.Save(fileout);
		}

		static void DoSmooting(LockBitmap lbin,LockBitmap lbot)
		{
			int ht = lbin.Height;
			int wt = lbin.Width;
			int total = ht*wt-1;
			int count = 0;

			var pp = Parallel.For(0,total,(i) => {
				int y = i / wt;
				int x = i % wt;
				Color nc = SmoothPixel(lbin,x,y);
				lbot.SetPixel(x,y,nc);
				System.Threading.Interlocked.Increment(ref count);
				if (count % 1000 == 0) {
					Debug.WriteLine(count+" / "+total);
				}
			});

			//for(int y=ht-1; y>=0; y--)
			//{
			//	for(int x=wt-1; x>=0; x--)
			//	{
			//		Color nc = SmoothPixel(lbin,x,y);
			//		lbot.SetPixel(x,y,nc);
			//	}
			//	Debug.WriteLine("y = "+y);
			//}
		}

		static Color SmoothPixel(LockBitmap lb,int px, int py)
		{
			Color start = lb.GetPixel(px,py);
			double bestlen = double.MaxValue;
			double bestang = double.NaN;
			double bestratio = 1;
			Color bestfc = start;
			Color bestbc = start;
			Point bestfpx = new Point(px,py);
			Point bestbpx = new Point(px,py);
			double ahigh = Math.PI;
			double alow = 0;

			for(int tries=1; tries <= 7; tries++)
			{
				double dang = (ahigh - alow)/3;
				for(double a = alow; a<ahigh; a+=dang)
				{
					Color fc;
					Color bc;

					Point fp = FindColorAlongRay(lb,a,px,py,false,start,out fc);
					Point bp = FindColorAlongRay(lb,a,px,py,true,start,out bc);

					double len = Dist(fp.X,fp.Y,bp.X,bp.Y);

					if (len < bestlen) {
						bestang = a;
						bestlen = len;
						bestfc = Between(fc,start,0.5);
						bestbc = Between(bc,start,0.5);
						bestfpx = fp;
						bestbpx = bp;
						double flen = Dist(px,py,fp.X,fp.Y);
						bestratio = flen/len;
					}
				}

				alow = bestang - Math.PI/3/tries;
				ahigh = bestang + Math.PI/3/tries;
			}

			//if (Math.Abs(px - bestfpx.X) < 2 && Math.Abs(py - bestfpx.Y) < 2) {
			//	return start;
			//}
			//if (Math.Abs(px - bestbpx.X) < 2 && Math.Abs(py - bestbpx.Y) < 2) {
			//	return start;
			//}
			if (bestfc == start && bestbc == start) {
				return start;
			}
			if (bestratio > 0.5) {
				return Between(start,bestbc,(bestratio-0.5)*2);
			} else {
				return Between(bestfc,start,bestratio*2);
			}
		}

		static Color Between(Color a, Color b, double ratio)
		{
			int nr = (int)Math.Round((1-ratio)*a.R + ratio*b.R,0);
			int ng = (int)Math.Round((1-ratio)*a.G + ratio*b.G,0);
			int nb = (int)Math.Round((1-ratio)*a.B + ratio*b.B,0);
			int na = (int)Math.Round((1-ratio)*a.A + ratio*b.A,0);
			return Color.FromArgb(na,nr,ng,nb);
		}

		//http://en.wikipedia.org/wiki/Methods_of_computing_square_roots
		public static double Sqrt(double z)
		{
			if (z == 0) { return 0; }
			DoubleLongUnion u;
			u.tmp = 0;
			u.f = z;
			//Console.WriteLine("a "+Convert.ToString(u.tmp,2));
			u.tmp -= 1L << 52; /* Subtract 2^m. */
			///Console.WriteLine("b "+Convert.ToString(u.tmp,2));
			u.tmp >>= 1; /* Divide by 2. */
			//Console.WriteLine("c "+Convert.ToString(u.tmp,2));
			u.tmp += 1L << 61; /* Add ((b + 1) / 2) * 2^m. */
			//Console.WriteLine("d "+Convert.ToString(u.tmp,2));
			return u.f;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct DoubleLongUnion
		{
			[FieldOffset(0)]
			public double f;

			[FieldOffset(0)]
			public long tmp;
		}

		//public static float Sqrt(float z)
		//{
		//	if (z == 0) return 0;
		//	FloatIntUnion u;
		//	u.tmp = 0;
		//	u.f = z;
		//	//Console.WriteLine("a "+Convert.ToString(u.tmp,2));
		//	u.tmp -= 1 << 23; /* Subtract 2^m. */
		//	//Console.WriteLine("b "+Convert.ToString(u.tmp,2));
		//	u.tmp >>= 1; /* Divide by 2. */
		//	//Console.WriteLine("c "+Convert.ToString(u.tmp,2));
		//	u.tmp += 1 << 29; /* Add ((b + 1) / 2) * 2^m. */
		//	//Console.WriteLine("d "+Convert.ToString(u.tmp,2));
		//	return u.f;
		//}

		//[StructLayout(LayoutKind.Explicit)]
		//private struct FloatIntUnion
		//{
		//	[FieldOffset(0)]
		//	public float f;

		//	[FieldOffset(0)]
		//	public int tmp;
		//}

		static double Dist(int x1,int y1,int x2,int y2)
		{
			int dx = x2 - x1;
			int dy = y2 - y1;
			//return Math.Sqrt(dx*dx + dy*dy);
			return Sqrt((double)dx*dx + (double)dy*dy);
		}

		static Point FindColorAlongRay(LockBitmap lb, double a, int px, int py, bool back, Color start, out Color c)
		{
			double r=1;
			c = start;
			bool done = false;
			double cosa = Math.Cos(a) * (back ? -1 : 1);
			double sina = Math.Sin(a) * (back ? -1 : 1);
			int maxx = lb.Width -1;
			int maxy = lb.Height -1;
			
			while(true) {
				int fx = (int)(cosa * r) + px;
				int fy = (int)(sina * r) + py;
				if (fx < 0 || fy < 0 || fx > maxx || fy > maxy) {
					done = true;
				}
				if (!done) {
					Color f = lb.GetPixel(fx,fy);
					if (f != start) {
						c = f;
						done = true;

					}
				}
				if (done) {
					return new Point(
						fx < 0 ? 0 : fx > maxx ? maxx : fx
						,fy < 0 ? 0 : fy > maxy ? maxy : fy
					);
				}
				r+=1;
			}
		}

		//static Color SmoothPixel(LockBitmap lb,int px, int py)
		//{
		//	Color cc = lb.GetPixel(px,py);
		//	Color gt = cc, lt = cc;
		//	int rad = 1;
		//	int d = 0, count = 0;
		//	int x = px, y = py;
		//	int ht = lb.Height;
		//	int wt = lb.Width;
		//	double ccmag = ColorMag(cc);
		//	double gtmag = ccmag, ltmag = ccmag;

		//	while(gt == cc && lt == cc)
		//	{
		//		while (count < rad)
		//		{
		//			switch(d)
		//			{
		//			case 0: y--; break;
		//			case 1: x--; break;
		//			case 2: y++; break;
		//			case 3: x++; break;
		//			}

		//			if (x > 0 && x < wt && y > 0 && y < ht)
		//			{
		//				Color c = lb.GetPixel(x,y);
		//				double cmag = ColorMag(c);

		//				if (cmag < ccmag && (ccmag - cmag) > ltmag) {
		//					lt = c;
		//					ltmag = ColorMag(lt);
		//				}
		//				if (cmag > ccmag && (cmag - ccmag) < gtmag) {
		//					gt = c;
		//					gtmag = ColorMag(gt);
		//				}
		//			}
		//		}
		//		count = 0;
		//		d++;
		//		if (d > 3) {
		//			rad++;
		//			d = 0;
		//		}

		//		if (rad > Math.Max(ht,wt)) {
		//			break;
		//		}
		//	}
		//}

		//static double ColorMag(Color c)
		//{
		//	return Math.Sqrt(c.R*c.R + c.G*c.G + c.B*c.B);
		//}
	}
}
