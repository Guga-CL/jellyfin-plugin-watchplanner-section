const path = require('path');
const { BundleAnalyzerPlugin } = require('webpack-bundle-analyzer');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');

module.exports = (env = {}, argv = {}) => {
  const isProduction = argv.mode === 'production';
  const isSingle = env && (env.SINGLE === 'true' || process.env.SINGLE === 'true');
  const shouldAnalyze = process.env.ANALYZE === 'true' || env.ANALYZE === 'true';

  const output = isSingle
    ? {
        filename: 'bundle.js',
        chunkFilename: 'bundle-[id].js',
        path: path.resolve(__dirname, 'dist'),
        publicPath: ''
      }
    : {
        filename: isProduction ? '[name].[contenthash].js' : '[name].js',
        chunkFilename: isProduction ? '[name].[contenthash].js' : '[name].js',
        path: path.resolve(__dirname, 'dist'),
        publicPath: ''
      };

  return {
    mode: isProduction ? 'production' : 'development',
    entry: path.resolve(__dirname, 'src', 'app-init.js'),
    output,
    resolve: { extensions: ['.js'] },
    devtool: isProduction ? false : 'eval-source-map',
    optimization: isSingle
      ? {}
      : {
          splitChunks: {
            chunks: 'all',
            cacheGroups: {
              vendor: {
                test: /[\\/]node_modules[\\/]/,
                name: 'vendors',
                chunks: 'all',
                enforce: true
              }
            }
          },
          runtimeChunk: { name: 'runtime' },
          chunkIds: 'deterministic'
        },
    // NO babel-loader rule — relying on modern runtime support
    module: {
      rules: []
    },
    plugins: [
      new CleanWebpackPlugin(),
      ...(shouldAnalyze ? [new BundleAnalyzerPlugin({ analyzerMode: 'static', openAnalyzer: false })] : [])
    ]
  };
};
