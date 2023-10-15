#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
using UnityEngine;

namespace Sisus.Debugging
{
	public class FpsCounter
	{
		private const int defaultSampleSize = 20;
		private const float updateFpsInterval = 1f;

		private readonly float[] samples = new float[20];

		private int sampleIndex;
		private float fps;

		private float nextUpdateFps = updateFpsInterval;

		public int Fps
		{
			get
			{
				return Mathf.RoundToInt(fps);
			}
		}

		public FpsCounter()
		{
			samples = new float[defaultSampleSize];
			for(int n = 19; n >= 0; n--)
			{
				samples[n] = 0.001f;
			}
		}

		public FpsCounter(int sampleSize = defaultSampleSize)
		{
			samples = new float[sampleSize];
			for(int n = sampleSize - 1; n >= 0; n--)
			{
				samples[n] = 0.001f;
			}
		}

		public void Update()
		{
			// this can occur while editor is entering or exiting play mode
			if(Time.smoothDeltaTime <= 0f)
			{
				return;
			}

			samples[sampleIndex] = 1f / Time.smoothDeltaTime;

			int lastSampleIndex = samples.Length - 1;
			sampleIndex = sampleIndex < lastSampleIndex ? sampleIndex + 1 : 0;

			nextUpdateFps -= Time.smoothDeltaTime;
			if(nextUpdateFps > 0f)
			{
				return;
			}
			nextUpdateFps = updateFpsInterval;

			fps = 0f;
			for(int n = lastSampleIndex; n >= 0; n--)
			{
				fps += samples[n];
			}
			fps /= (lastSampleIndex + 1);
		}
	}
}
#endif