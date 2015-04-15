//-----------------------------------------------------------------------
// <copyright file="BarcodeDraw.cs" company="Zen Design Corp">
//     Copyright © Zen Design Corp 2008 - 2012. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Drawing;
using System.Windows;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Size = System.Windows.Size;

namespace Zen.Barcode
{
	using System;

	/// <summary>
	/// <c>BarcodeMetrics</c> defines the measurement metrics used to render
	/// a barcode.
	/// </summary>
	[Serializable]
	public abstract class BarcodeMetrics
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BarcodeMetrics"/> class.
		/// </summary>
		protected BarcodeMetrics()
		{
			Scale = 1;
		}

		/// <summary>
		/// Gets or sets the scale factor used to render a barcode.
		/// </summary>
		/// <value>The scale.</value>
		/// <remarks>
		/// When applied to a 1D barcode the scale is used to scale the width
		/// of barcode elements not the height.
		/// When applied to a 2D barcode the scale adjusts both width and height
		/// of barcode elements.
		/// </remarks>
		public int Scale
		{
			get;
			set;
		}
	}

	/// <summary>
	/// <c>BarcodeMetrics1d</c> defines the measurement metrics used to render
	/// a 1 dimensional barcode.
	/// </summary>
	[Serializable]
	public class BarcodeMetrics1d : BarcodeMetrics
	{
		#region Private Fields
        private double _minBarWidth;
        private double _maxBarWidth;
        private double _minBarHeight;
        private double _maxBarHeight;
		private int? _interGlyphSpacing;
		private bool _renderVertically;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="BarcodeMetrics1d"/> class.
		/// </summary>
		public BarcodeMetrics1d()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BarcodeMetrics1d"/> class.
		/// </summary>
		/// <param name="barWidth"></param>
		/// <param name="barHeight"></param>
        public BarcodeMetrics1d(double barWidth, double barHeight)
		{
			_minBarWidth = _maxBarWidth = barWidth;
			_minBarHeight = _maxBarHeight = barHeight;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BarcodeMetrics1d"/> class.
		/// </summary>
		/// <param name="minBarWidth"></param>
		/// <param name="maxBarWidth"></param>
		/// <param name="barHeight"></param>
        public BarcodeMetrics1d(double minBarWidth, double maxBarWidth, double barHeight)
		{
			_minBarWidth = minBarWidth;
			_maxBarWidth = maxBarWidth;
			_minBarHeight = _maxBarHeight = barHeight;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BarcodeMetrics1d"/> class.
		/// </summary>
		/// <param name="minBarWidth"></param>
		/// <param name="maxBarWidth"></param>
		/// <param name="minBarHeight"></param>
		/// <param name="maxBarHeight"></param>
		public BarcodeMetrics1d(
            double minBarWidth, double maxBarWidth, double minBarHeight, double maxBarHeight)
		{
			_minBarWidth = minBarWidth;
			_maxBarWidth = maxBarWidth;
			_minBarHeight = minBarHeight;
			_maxBarHeight = maxBarHeight;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets/sets the minimum bar width.
		/// </summary>
        public double MinBarWidth
		{
			get
			{
				return _minBarWidth;
			}
			set
			{
				_minBarWidth = value;
			}
		}

		/// <summary>
		/// Gets/sets the maximum bar width.
		/// </summary>
        public double MaxBarWidth
		{
			get
			{
				return _maxBarWidth;
			}
			set
			{
				_maxBarWidth = value;
			}
		}

		/// <summary>
		/// Gets/sets the minimum bar height.
		/// </summary>
        public double MinBarHeight
		{
			get
			{
				return _minBarHeight;
			}
			set
			{
				_minBarHeight = value;
			}
		}

		/// <summary>
		/// Gets/sets the maximum bar height.
		/// </summary>
        public double MaxBarHeight
		{
			get
			{
				return _maxBarHeight;
			}
			set
			{
				_maxBarHeight = value;
			}
		}

		/// <summary>
		/// Gets/sets the amount of inter-glyph spacing to apply.
		/// </summary>
		/// <remarks>
		/// By default this is set to -1 which forces the barcode drawing
		/// classes to use the default value specified by the symbology.
		/// </remarks>
		public int? InterGlyphSpacing
		{
			get
			{
				return _interGlyphSpacing;
			}
			set
			{
				_interGlyphSpacing = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to render the barcode vertically.
		/// </summary>
		/// <value>
		/// <c>true</c> to render barcode vertically; otherwise, <c>false</c>.
		/// </value>
		public bool RenderVertically
		{
			get
			{
				return _renderVertically;
			}
			set
			{
				_renderVertically = value;
			}
		}
		#endregion
	}

	/// <summary>
	/// <c>BarcodeDraw</c> is an abstract base class for all barcode drawing
	/// classes.
	/// </summary>
	public abstract class BarcodeDraw
	{
		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of <see cref="T:Zen.Barcode.BarcodeDraw"/> class.
		/// </summary>
		protected BarcodeDraw()
		{
		}
		#endregion

		#region Public Methods

	    /// <summary>
	    /// Draws the specified text using the supplied barcode metrics.
	    /// </summary>
	    /// <param name="dc"></param>
	    /// <param name="text">The text.</param>
	    /// <param name="metrics">A <see cref="T:Zen.Barcode.BarcodeMetrics"/> object.</param>
	    /// <param name="bounds"></param>
	    /// <returns>
	    /// An <see cref="ImageSource"/> object containing the rendered barcode.
	    /// </returns>
	    public abstract void Draw(DrawingContext dc, string text, BarcodeMetrics metrics, Rect bounds);

	    /// <summary>
	    /// Draws the specified text using the default barcode metrics for
	    /// the specified maximum barcode height.
	    /// </summary>
	    /// <param name="dc"></param>
	    /// <param name="text">The text.</param>
	    /// <param name="maxBarHeight">The maximum bar height.</param>
	    /// <param name="bounds"></param>
	    /// <returns>
	    /// An <see cref="ImageSource"/> object containing the rendered barcode.
	    /// </returns>
	    public void Draw(DrawingContext dc, string text, double maxBarHeight, Rect bounds)
		{
			var defaultMetrics = GetDefaultMetrics(maxBarHeight);
			Draw(dc, text, defaultMetrics, bounds);
		}

	    /// <summary>
	    /// Draws the specified text using the default barcode metrics for
	    /// the specified maximum barcode height.
	    /// </summary>
	    /// <param name="dc"></param>
	    /// <param name="text">The text.</param>
	    /// <param name="maxBarHeight">The maximum bar height.</param>
	    /// <param name="scale">
	    /// The scale factor to use when rendering the barcode.
	    /// </param>
	    /// <param name="bounds"></param>
	    /// <returns>
	    /// An <see cref="ImageSource"/> object containing the rendered barcode.
	    /// </returns>
	    public void Draw(DrawingContext dc, string text, int maxBarHeight, int scale, Rect bounds)
		{
			var defaultMetrics = GetDefaultMetrics(maxBarHeight);
			defaultMetrics.Scale = scale;
			Draw(dc, text, defaultMetrics, bounds);
		}

		/// <summary>
		/// Gets a <see cref="T:Zen.Barcode.BarcodeMetrics"/> object containing default
		/// settings for the specified maximum bar height.
		/// </summary>
		/// <param name="maxHeight">The maximum barcode height.</param>
		/// <returns>A <see cref="T:Zen.Barcode.BarcodeMetrics"/> object.</returns>
		public abstract BarcodeMetrics GetDefaultMetrics(double maxHeight);

		/// <summary>
		/// Gets a <see cref="T:BarcodeMetrics"/> object containing the print
		/// metrics needed for printing a barcode of the specified physical
		/// size on a device operating at the specified resolution.
		/// </summary>
		/// <param name="desiredBarcodeDimensions">The desired barcode dimensions in hundredth of an inch.</param>
		/// <param name="printResolution">The print resolution in pixels per inch.</param>
		/// <param name="barcodeCharLength">Length of the barcode in characters.</param>
		/// <returns>A <see cref="T:Zen.Barcode.BarcodeMetrics"/> object.</returns>
		public abstract BarcodeMetrics GetPrintMetrics(
			Size desiredBarcodeDimensions, Size printResolution,
			int barcodeCharLength);
		#endregion
	}

	/// <summary>
	/// <b>BarcodeDrawBase</b> deals with rendering a barcode using the associated
	/// glyph factory and optional checksum generator classes.
	/// </summary>
	/// <typeparam name="TGlyphFactory">
	/// A <see cref="T:Zen.Barcode.GlyphFactory"/> derived type.
	/// </typeparam>
	/// <typeparam name="TChecksum">
	/// A <see cref="T:Zen.Barcode.Checksum"/> derived type.
	/// </typeparam>
	public abstract class BarcodeDrawBase<TGlyphFactory, TChecksum> : BarcodeDraw
		where TGlyphFactory : GlyphFactory
		where TChecksum : Checksum
	{

		#region Private Fields
		private TGlyphFactory _factory;
		private TChecksum _checksum;
		private int _encodingBitCount;
		private int _widthBitCount;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initialises an instance of <see cref="T:Zen.Barcode.BarcodeDraw"/>.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="encodingBitCount">
		/// Number of bits in each encoded glyph.
		/// Set to <c>0</c> for variable length bit encoded glyphs.
		/// </param>
		protected BarcodeDrawBase(TGlyphFactory factory, int encodingBitCount)
		{
			_factory = factory;
			_encodingBitCount = encodingBitCount;
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:Zen.Barcode.BarcodeDraw"/>.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="encodingBitCount">
		/// Number of bits in each encoded glyph.
		/// Set to <c>0</c> for variable length bit encoded glyphs.
		/// </param>
		/// <param name="widthBitCount">Width of the width bit.</param>
		protected BarcodeDrawBase(TGlyphFactory factory, int encodingBitCount,
			int widthBitCount)
		{
			_factory = factory;
			_encodingBitCount = encodingBitCount;
			_widthBitCount = widthBitCount;
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:Zen.Barcode.BarcodeDraw"/>.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="checksum">The checksum.</param>
		/// <param name="encodingBitCount">
		/// Number of bits in each encoded glyph.
		/// Set to <c>0</c> for variable length bit encoded glyphs.
		/// </param>
		protected BarcodeDrawBase(TGlyphFactory factory, TChecksum checksum,
			int encodingBitCount)
		{
			_factory = factory;
			_checksum = checksum;
			_encodingBitCount = encodingBitCount;
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:Zen.Barcode.BarcodeDraw"/>.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="checksum">The checksum.</param>
		/// <param name="encodingBitCount">
		/// Number of bits in each encoded glyph.
		/// Set to <c>0</c> for variable length bit encoded glyphs.
		/// </param>
		/// <param name="widthBitCount">Width of the width bit.</param>
		protected BarcodeDrawBase(TGlyphFactory factory, TChecksum checksum,
			int encodingBitCount, int widthBitCount)
		{
			_factory = factory;
			_checksum = checksum;
			_encodingBitCount = encodingBitCount;
			_widthBitCount = widthBitCount;
		}
		#endregion

		#region Public Methods

	    /// <summary>
	    /// Draws the specified text using the supplied barcode metrics.
	    /// </summary>
	    /// <param name="dc"></param>
	    /// <param name="text">The text.</param>
	    /// <param name="metrics">A <see cref="T:Zen.Barcode.BarcodeMetrics"/> object.</param>
	    /// <param name="bounds"></param>
	    /// <returns></returns>
	    public override sealed void Draw(DrawingContext dc, string text, BarcodeMetrics metrics, Rect bounds)
		{
			Draw1d(dc, text, (BarcodeMetrics1d)metrics, bounds);
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the <typeparamref name="TGlyphFactory"/> glyph factory.
		/// </summary>
		/// <value>The <typeparamref name="TGlyphFactory" /> factory.</value>
		protected TGlyphFactory Factory
		{
			get
			{
				return _factory;
			}
		}

		/// <summary>
		/// Gets the <typeparamref name="TChecksum"/> checksum object.
		/// </summary>
		/// <value>The <typeparamref name="TChecksum" /> checksum.</value>
		protected TChecksum Checksum
		{
			get
			{
				return _checksum;
			}
		}

		/// <summary>
		/// Gets the number of bits used for a glyph encoding.
		/// </summary>
		/// <value>Number of bits used to represent the glyph encoding.</value>
		protected int EncodingBitCount
		{
			get
			{
				return _encodingBitCount;
			}
		}

		/// <summary>
		/// Gets the number of bits used to encode the glyph width 
		/// information.
		/// </summary>
		/// <value>Number of bits used to represent bar width encoding.</value>
		protected int WidthBitCount
		{
			get
			{
				return _widthBitCount;
			}
		}
		#endregion

		#region Protected Methods

	    /// <summary>
	    /// Draws the specified text using the supplied barcode metrics.
	    /// </summary>
	    /// <param name="dc"></param>
	    /// <param name="text">The text.</param>
	    /// <param name="metrics">A <see cref="T:Zen.Barcode.BarcodeMetrics"/> object.</param>
	    /// <param name="bounds"></param>
	    /// <returns>
	    /// An <see cref="ImageSource"/> object containing the rendered barcode.
	    /// </returns>
	    protected virtual void Draw1d(DrawingContext dc, string text, BarcodeMetrics1d metrics, Rect bounds)
		{
			// Determine number of pixels required for final image
			Glyph[] barcode = GetFullBarcode(text);

			// Determine amount of inter-glyph space
            double interGlyphSpace;
			if (metrics.InterGlyphSpacing.HasValue)
			{
				interGlyphSpace = metrics.InterGlyphSpacing.Value;
			}
			else
			{
				interGlyphSpace = GetDefaultInterGlyphSpace(
					metrics.MinBarWidth, metrics.MaxBarWidth);
			}

			// Determine bar code length in pixels
            var totalImageWidth = GetBarcodeLength(
				barcode,
				interGlyphSpace * metrics.Scale,
				metrics.MinBarWidth * metrics.Scale,
				metrics.MaxBarWidth * metrics.Scale);
			
			var drawbounds = CalculateDrawBounds(bounds, totalImageWidth, metrics.MaxBarHeight);
			Render(
				barcode,
				dc,
				drawbounds,
				interGlyphSpace * metrics.Scale,
				metrics.MinBarHeight,
				metrics.MinBarWidth * metrics.Scale,
				metrics.MaxBarWidth * metrics.Scale);
		}

        private static Rect CalculateDrawBounds(Rect bounds, double totalImageWidth, double maxBarHeight)
        {
            var height = maxBarHeight > bounds.Height ? bounds.Height : maxBarHeight;
            var width = totalImageWidth > bounds.Width ? bounds.Width : totalImageWidth;

            var top = bounds.Top + (bounds.Height - height)/2d;
            var left = bounds.Left + (bounds.Width - width)/2d;
            return new Rect(left, top, width, height);
        }

		/// <summary>
		/// Gets the default amount of inter-glyph space to apply.
		/// </summary>
		/// <param name="barMinWidth">The min bar width in pixels.</param>
		/// <param name="barMaxWidth">The max bar width in pixels.</param>
		/// <returns>The amount of inter-glyph spacing to apply in pixels.</returns>
		/// <remarks>
		/// By default this method returns zero.
		/// </remarks>
        protected virtual double GetDefaultInterGlyphSpace(
            double barMinWidth, double barMaxWidth)
		{
			return 0;
		}

		/// <summary>
		/// Gets the glyphs needed to render a full barcode.
		/// </summary>
		/// <param name="text">Text to convert into bar-code.</param>
		/// <returns>A collection of <see cref="T:Zen.Barcode.Glyph"/> objects.</returns>
		protected abstract Glyph[] GetFullBarcode(string text);

		/// <summary>
		/// Gets the length in pixels needed to render the specified barcode.
		/// </summary>
		/// <param name="barcode">Barcode glyphs to be analysed.</param>
		/// <param name="interGlyphSpace">Amount of inter-glyph space.</param>
		/// <param name="barMinWidth">Minimum barcode width.</param>
		/// <param name="barMaxWidth">Maximum barcode width.</param>
		/// <returns>The barcode width in pixels.</returns>
		/// <remarks>
		/// Currently this method does not account for any "quiet space"
		/// around the barcode as dictated by each symbology standard.
		/// </remarks>
        protected virtual double GetBarcodeLength(
            Glyph[] barcode, double interGlyphSpace, double barMinWidth, double barMaxWidth)
		{
			// Determine bar code length in pixels
            double totalImageWidth = GetBarcodeInterGlyphLength(barcode, interGlyphSpace);
			foreach (BarGlyph glyph in barcode)
			{
				// Determine encoding bit-width for this character
				int encodingBitCount = GetEncodingBitCount(glyph);
				if (glyph is IBinaryPitchGlyph)
				{
					IBinaryPitchGlyph binaryGlyph = (IBinaryPitchGlyph)glyph;
					int widthIndex = WidthBitCount - 1;
					bool lastBitState = false;
					for (int bitIndex = encodingBitCount - 1; bitIndex >= 0; --bitIndex)
					{
						// Determine whether the bit state is changing
						int bitmask = (1 << bitIndex);
						bool currentBitState = false;
						if ((bitmask & binaryGlyph.BitEncoding) != 0)
						{
							currentBitState = true;
						}

						// Adjust the width bit checker
						if (bitIndex < (encodingBitCount - 1) &&
							lastBitState != currentBitState)
						{
							--widthIndex;
						}
						lastBitState = currentBitState;

						// Determine width encoding bit mask
						bitmask = (1 << widthIndex);
						if ((bitmask & binaryGlyph.WidthEncoding) != 0)
						{
							totalImageWidth += barMaxWidth;
						}
						else
						{
							totalImageWidth += barMinWidth;
						}
					}
				}
				else
				{
					totalImageWidth += (encodingBitCount * barMinWidth);
				}
			}
			return totalImageWidth;
		}

		/// <summary>
		/// Gets the glyph's barcode encoding bit count.
		/// </summary>
		/// <param name="glyph">A <see cref="T:Zen.Barcode.Glyph"/> to be queried.</param>
		/// <returns>Number of bits needed to encode the glyph.</returns>
		/// <remarks>
		/// By default this method returns the underlying encoding bit width.
		/// If the glyph implements <see cref="T:Zen.Barcode.IVaryLengthGlyph"/> then the
		/// encoding width is requested from the interface.
		/// </remarks>
		protected virtual int GetEncodingBitCount(Glyph glyph)
		{
			int bitEncodingWidth = this.EncodingBitCount;
			if (glyph is IVaryLengthGlyph)
			{
				IVaryLengthGlyph varyLengthGlyph = (IVaryLengthGlyph)glyph;
				bitEncodingWidth = varyLengthGlyph.BitEncodingWidth;
			}
			return bitEncodingWidth;
		}

		/// <summary>
		/// Gets the glyph's width encoding bit count.
		/// </summary>
		/// <param name="glyph">A <see cref="T:Zen.Barcode.Glyph"/> to be queried.</param>
		/// <returns>Number of bits needed to encode the width of the glyph.</returns>
		/// <remarks>
		/// By default this method returns the underlying width bit count.
		/// </remarks>
		protected virtual int GetWidthBitCount(Glyph glyph)
		{
			int widthBitCount = this.WidthBitCount;
			return widthBitCount;
		}

		/// <summary>
		/// Gets the total width in pixels for the specified barcode glyphs
		/// incorporating the specified inter-glyph spacing.
		/// </summary>
		/// <param name="barcode">
		/// Collection of <see cref="T:Zen.Barcode.Glyph"/> objects to be rendered.
		/// </param>
		/// <param name="interGlyphSpace">Amount of inter-glyph space (in pixels) to be applied.</param>
		/// <returns>Width in pixels.</returns>
        protected double GetBarcodeInterGlyphLength(Glyph[] barcode,
            double interGlyphSpace)
		{
			return ((barcode.Length - 1) * interGlyphSpace);
		}

		/// <summary>
		/// Renders the specified bar-code to the specified graphics port.
		/// </summary>
		/// <param name="barcode">A collection of <see cref="T:Zen.Barcode.Glyph"/> objects representing the
		/// barcode to be rendered.</param>
		/// <param name="dc">A <see cref="T:System.Drawing.Graphics"/> representing the draw context.</param>
		/// <param name="bounds">The bounding rectangle.</param>
		/// <param name="interGlyphSpace">The inter glyph space in pixels.</param>
		/// <param name="barMinHeight">Minimum bar height in pixels.</param>
		/// <param name="barMinWidth">Small bar width in pixels.</param>
		/// <param name="barMaxWidth">Large bar width in pixels.</param>
		/// <remarks>
		/// This method clears the background and then calls
		/// <see cref="M:RenderBars"/> to perform the actual bar drawing.
		/// </remarks>
		protected virtual void Render(
			Glyph[] barcode,
			DrawingContext dc,
			Rect bounds,
            double interGlyphSpace,
            double barMinHeight,
            double barMinWidth,
            double barMaxWidth)
		{
			// Render the background
            dc.DrawRectangle(Brushes.White, null, bounds);

			// Render the bars
			RenderBars(barcode, dc, bounds, interGlyphSpace, barMinHeight,
				barMinWidth, barMaxWidth);
		}

	    /// <summary>
		/// Renders the barcode bars.
		/// </summary>
		/// <param name="barcode">A collection of <see cref="T:Zen.Barcode.Glyph"/> objects representing the
		/// barcode to be rendered.</param>
		/// <param name="dc">A <see cref="T:System.Drawing.Graphics"/> representing the draw context.</param>
		/// <param name="bounds">The bounding rectangle.</param>
		/// <param name="interGlyphSpace">The inter glyph space in pixels.</param>
		/// <param name="barMinHeight">Minimum bar height in pixels.</param>
		/// <param name="barMinWidth">Small bar width in pixels.</param>
		/// <param name="barMaxWidth">Large bar width in pixels.</param>
		/// <remarks>
		/// By default this method renders each glyph by calling the
		/// <see cref="M:RenderBar"/> method, applying the specified
		/// inter-glyph spacing as necessary.
		/// </remarks>
		protected virtual void RenderBars(
			Glyph[] barcode,
			DrawingContext dc,
			Rect bounds,
            double interGlyphSpace,
            double barMinHeight,
            double barMinWidth,
            double barMaxWidth)
		{
            double barOffset = 0;
			for (int index = 0; index < barcode.Length; ++index)
			{
				BarGlyph glyph = (BarGlyph)barcode[index];

				RenderBar(index, glyph, dc, bounds, ref barOffset, barMinHeight,
					barMinWidth, barMaxWidth);

				// Account for inter glyph spacing
				barOffset += interGlyphSpace;
			}
		}

		/// <summary>
		/// Renders the bar-code glyph.
		/// </summary>
		/// <param name="glyphIndex">Index of the glyph.</param>
		/// <param name="glyph">A <see cref="T:Zen.Barcode.Glyph"/> object to be rendered.</param>
		/// <param name="dc">A <see cref="T:System.Drawing.Graphics"/> representing the draw context.</param>
		/// <param name="bounds">The bounding rectangle.</param>
		/// <param name="barOffset">The bar offset.</param>
		/// <param name="barMinHeight">Minimum bar height in pixels.</param>
		/// <param name="barMinWidth">Small bar width in pixels.</param>
		/// <param name="barMaxWidth">Large bar width in pixels.</param>
		/// <exception cref="T:System.InvalidOperationException">
		/// Thrown if the encoding bit count is zero or variable-pitch
		/// bar rendering is attempted.
		/// </exception>
		protected virtual void RenderBar(
			int glyphIndex,
			BarGlyph glyph,
			DrawingContext dc,
			Rect bounds,
            ref double barOffset,
            double barMinHeight,
            double barMinWidth,
            double barMaxWidth)
		{
			// Sanity check
			int encodingBitCount = GetEncodingBitCount(glyph);
			if (encodingBitCount == 0)
			{
				throw new InvalidOperationException(
					"Encoding bit width must be greater than zero.");
			}

			// Allow derived classes to modify the glyph bits
			int glyphBits = GetGlyphEncoding(glyphIndex, glyph);

			// Get glyph height
			var height = GetGlyphHeight(glyph, barMinHeight, bounds.Height);
			if (glyph is IBinaryPitchGlyph)
			{
				IBinaryPitchGlyph binGlyph = (IBinaryPitchGlyph)glyph;

				// Render glyph
				int widthIndex = WidthBitCount - 1;
				bool lastBitState = false;
				for (int bitIndex = encodingBitCount - 1; bitIndex >= 0; --bitIndex)
				{
					int bitMask = 1 << bitIndex;
                    double barWidth = barMinWidth;

					bool currentBitState = false;
					if ((bitMask & glyphBits) != 0)
					{
						currentBitState = true;
					}

					// Adjust the width bit checker
					if (bitIndex < (encodingBitCount - 1) &&
						lastBitState != currentBitState)
					{
						--widthIndex;
					}
					lastBitState = currentBitState;

					// Determine width encoding bit mask
					int widthMask = (1 << widthIndex);
					if ((widthMask & binGlyph.WidthEncoding) != 0)
					{
						barWidth = barMaxWidth;
					}

					if ((binGlyph.BitEncoding & bitMask) != 0)
					{
                        dc.DrawRectangle(Brushes.Black, null, new Rect(bounds.Left + barOffset, bounds.Top,
							barWidth, height));
					}

					// Update offset
					barOffset += barWidth;
				}
			}
			else
			{
				for (int bitIndex = encodingBitCount - 1; bitIndex >= 0; --bitIndex)
				{
					int bitMask = (1 << bitIndex);
					if ((glyphBits & bitMask) != 0)
					{
						dc.DrawRectangle(Brushes.Black, null, new Rect(bounds.Left + barOffset, bounds.Top,
							barMinWidth, height));
					}

					// Update offset
					barOffset += barMinWidth;
				}
			}
		}

		/// <summary>
		/// Gets the glyph encoding.
		/// </summary>
		/// <param name="glyphIndex">Index of the glyph.</param>
		/// <param name="glyph">The glyph.</param>
		/// <returns></returns>
		/// <remarks>
		/// By default this method simply returns the glyph bit encoding
		/// however some algorithms may chose to modify the encoding
		/// based on positional information.
		/// </remarks>
		protected virtual int GetGlyphEncoding(int glyphIndex, BarGlyph glyph)
		{
			return glyph.BitEncoding;
		}

		/// <summary>
		/// Gets the height of the glyph.
		/// </summary>
		/// <param name="glyph">A <see cref="T:Zen.Barcode.Glyph"/> to be queried.</param>
		/// <param name="barMinHeight">Minimum bar height in pixels.</param>
		/// <param name="barMaxHeight">Maximum bar height in pixels.</param>
		/// <returns>The height of associated glyph.</returns>
		/// <remarks>
		/// By default this method returns the maximum bar height.
		/// </remarks>
		protected virtual double GetGlyphHeight(Glyph glyph, double barMinHeight, double barMaxHeight)
		{
			return barMaxHeight;
		}
		#endregion
	}
}
